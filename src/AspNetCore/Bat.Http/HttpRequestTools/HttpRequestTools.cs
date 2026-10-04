namespace Bat.Http;

public static class HttpRequestTools
{
    private static string BuildQueryString(Dictionary<string, string> parameters)
    {
        if (parameters is null || parameters.Count == 0)
            return string.Empty;

        var queryParams = parameters.Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}");

        return string.Join("&", queryParams);
    }

    private static string BuildQueryString(object parameter, Type objectType)
    {
        if (parameter is null)
            return string.Empty;

        var fields = parameter.GetClassFields(objectType);
        if (fields is null || fields.Any() is false)
            return string.Empty;

        var queryParams = fields.Select(field =>
            $"{Uri.EscapeDataString(field.Name)}={Uri.EscapeDataString(field.Value?.ToString() ?? string.Empty)}");

        return string.Join("&", queryParams);
    }

    private static string BuildCompleteUrl(string baseUrl, string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
            return baseUrl;

        return $"{baseUrl}?{queryString}";
    }

    private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string> headers)
    {
        if (headers is null || headers.Count == 0)
            return;

        foreach (var header in headers)
        {
            // TryAddWithoutValidation is better than Add because it doesn't throw exceptions
            // for headers that might need to be added to Content.Headers instead
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    #region Shared HttpClient
    // Previously every call created (and disposed) a new HttpClient/HttpClientHandler. That opens a new TCP/TLS
    // connection per request, leaves sockets in TIME_WAIT (port exhaustion under load) and defeats connection pooling.
    // Two long-lived clients are used instead (normal and "bypass certificate validation"). PooledConnectionLifetime
    // makes pooled connections pick up DNS changes. Cookies are disabled so that nothing leaks between unrelated
    // calls (each call used to get a fresh, empty cookie container).
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(100); // HttpClient's default timeout
    private static readonly Lazy<HttpClient> _defaultClient = new(() => CreateClient(bypassCertificate: false));
    private static readonly Lazy<HttpClient> _bypassCertificateClient = new(() => CreateClient(bypassCertificate: true));

    private static HttpClient CreateClient(bool bypassCertificate)
    {
        var handler = new SocketsHttpHandler
        {
            UseCookies = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
        };

        if (bypassCertificate)
            handler.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, policyErrors) => true;

        // Timeouts are applied per request (see SendAsync) because a shared client's Timeout cannot change.
        return new HttpClient(handler, disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
    }

    private static HttpClient SharedClient(bool bypassCertificate)
        => bypassCertificate ? _bypassCertificateClient.Value : _defaultClient.Value;

    private static bool IsShared(HttpClient httpClient)
        => (_defaultClient.IsValueCreated && ReferenceEquals(httpClient, _defaultClient.Value))
        || (_bypassCertificateClient.IsValueCreated && ReferenceEquals(httpClient, _bypassCertificateClient.Value));

    /// <summary>
    /// Sends the request with a per-request timeout. Setting HttpClient.Timeout (what the old code did on
    /// caller-supplied clients) throws InvalidOperationException once the client has sent its first request,
    /// and would change the timeout for every other user of that client.
    /// </summary>
    private static async Task<HttpResponseMessage> SendAsync(HttpClient httpClient, HttpRequestMessage request, int? timeOutSecond, CancellationToken cancellationToken)
    {
        TimeSpan? timeout = timeOutSecond is > 0
            ? TimeSpan.FromSeconds(timeOutSecond.Value)
            : (IsShared(httpClient) ? _defaultTimeout : null);

        if (timeout is null)
            return await httpClient.SendAsync(request, cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout.Value);
        try
        {
            return await httpClient.SendAsync(request, timeoutCts.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Same exception shape HttpClient uses for its own timeout.
            throw new TaskCanceledException($"The request was canceled due to the configured timeout of {timeout.Value.TotalSeconds} seconds elapsing.", new TimeoutException(ex.Message, ex));
        }
    }
    #endregion


    public static bool IsAjaxRequest(this HttpRequest request)
    {
        if (request.Headers != null) return request.Headers["X-Requested-With"] == "XMLHttpRequest";

        return false;
    }


    public static async Task<T> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        var httpClient = SharedClient(false);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        var httpClient = SharedClient(false);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> GetAsync<T>(string url, string mediaType = "application/json", CancellationToken cancellationToken = default) where T : class
    {
        // The Accept header is set per request: the client is shared, so DefaultRequestHeaders must not be mutated.
        var httpClient = SharedClient(false);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync(cancellationToken);
        return result.DeSerializeJson<T>();
    }

    public static async Task<T> GetAsync<T>(string url, object parameter, Type objectType, CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(parameter, objectType);
        var completeUrl = BuildCompleteUrl(url, queryString);

        var httpClient = SharedClient(false);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(completeUrl));
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> GetAsync(string url, object parameter, Type objectType, CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(parameter, objectType);
        var completeUrl = BuildCompleteUrl(url, queryString);

        var httpClient = SharedClient(false);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(completeUrl));
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> GetAsync<T>(string url, Dictionary<string, string> parameter, CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(parameter);
        var completeUrl = BuildCompleteUrl(url, queryString);

        var httpClient = SharedClient(false);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(completeUrl));
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> GetAsync(string url, Dictionary<string, string> parameter, CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(parameter);
        var completeUrl = BuildCompleteUrl(url, queryString);

        var httpClient = SharedClient(false);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(completeUrl));
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> GetAsync<T>(string url, Dictionary<string, string> parameter, Dictionary<string, string> header, CancellationToken cancellationToken = default) where T : class
    {
        var queryString = BuildQueryString(parameter);
        var completeUrl = BuildCompleteUrl(url, queryString);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(completeUrl));
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> GetAsync(string url, Dictionary<string, string> parameter, Dictionary<string, string> header, CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(parameter);
        var completeUrl = BuildCompleteUrl(url, queryString);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(completeUrl));
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> GetAsync<T>(string url, Dictionary<string, string> parameter, Dictionary<string, string> header, bool byPassServerSertificate, int timeOutSecond, CancellationToken cancellationToken = default) where T : class
    {
        var queryString = BuildQueryString(parameter);
        var completeUrl = BuildCompleteUrl(url, queryString);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(completeUrl));
        AddHeaders(request, header);

        var httpClient = SharedClient(byPassServerSertificate);
        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> GetAsync(string url, Dictionary<string, string> parameter, Dictionary<string, string> header, bool byPassServerSertificate, int timeOutSecond, CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(parameter);
        var completeUrl = BuildCompleteUrl(url, queryString);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(completeUrl));
        AddHeaders(request, header);

        var httpClient = SharedClient(byPassServerSertificate);
        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }


    public static async Task<T> GetAsync<T>(HttpClient httpClient, string url, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<T> GetAsync<T>(HttpClient httpClient, string url, Dictionary<string, string> parameter, CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(parameter);
        var completeUrl = BuildCompleteUrl(url, queryString);

        using var response = await httpClient.GetAsync(completeUrl, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> GetAsync(HttpClient httpClient, string url, Dictionary<string, string> parameter, CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(parameter);
        var completeUrl = BuildCompleteUrl(url, queryString);

        using var response = await httpClient.GetAsync(completeUrl, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> GetAsync<T>(HttpClient httpClient, string url, object parameter, Type objectType, CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(parameter, objectType);
        var completeUrl = BuildCompleteUrl(url, queryString);

        using var response = await httpClient.GetAsync(completeUrl, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<T> GetAsync<T>(HttpClient httpClient, string url, Dictionary<string, string> parameter, Dictionary<string, string> header, CancellationToken cancellationToken = default) where T : class
    {
        var queryString = BuildQueryString(parameter);
        var completeUrl = BuildCompleteUrl(url, queryString);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(completeUrl));
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> GetAsync(HttpClient httpClient, string url, Dictionary<string, string> parameter, Dictionary<string, string> header, CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(parameter);
        var completeUrl = BuildCompleteUrl(url, queryString);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(completeUrl));
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> GetAsync<T>(HttpClient httpClient, string url, Dictionary<string, string> parameter, Dictionary<string, string> header, int timeOutSecond, CancellationToken cancellationToken = default) where T : class
    {
        var queryString = BuildQueryString(parameter);
        var completeUrl = BuildCompleteUrl(url, queryString);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(completeUrl));
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> GetAsync(HttpClient httpClient, string url, Dictionary<string, string> parameter, Dictionary<string, string> header, int timeOutSecond, CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(parameter);
        var completeUrl = BuildCompleteUrl(url, queryString);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(completeUrl));
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }




    public static async Task<T> PostAsync<T>(string url, object contentValues, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(contentValues.SerializeToJson(), resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostAsync(string url, object contentValues, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(contentValues.SerializeToJson(), resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> PostAsync<T>(string url, string contentJsonString, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostAsync(string url, string contentJsonString, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> PostAsync<T>(string url, string contentJsonString, Dictionary<string, string> header, Encoding resultEncoding, bool byPassServerSertificate, int timeOutSecond, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(byPassServerSertificate);
        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostAsync(string url, string contentJsonString, Dictionary<string, string> header, Encoding resultEncoding, bool byPassServerSertificate, int timeOutSecond, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(byPassServerSertificate);
        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> PostFormAsync<T>(string url, Dictionary<string, string> formBody, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default)
    {
        var formData = new MultipartFormDataContent();
        if (formBody is not null)
            foreach (var item in formBody)
                formData.Add(new StringContent(item.Value, resultEncoding ?? Encoding.UTF8), item.Key);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = formData
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostFormAsync(string url, Dictionary<string, string> formBody, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default)
    {
        var formData = new MultipartFormDataContent();
        if (formBody is not null)
            foreach (var item in formBody)
                formData.Add(new StringContent(item.Value, resultEncoding ?? Encoding.UTF8), item.Key);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = formData
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostFormFileAsync(string url, byte[] fileBytes, string fileName, Dictionary<string, string> header = null, bool byPassServerCertificate = true, CancellationToken cancellationToken = default)
    {
        var formData = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
        formData.Add(fileContent, "file", fileName);

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri(url))
        {
            Content = formData
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(byPassServerCertificate);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostFormFileAsync(string url, byte[] fileBytes, string fileName, string fileMediaType, Dictionary<string, string> header = null, bool byPassServerCertificate = true, CancellationToken cancellationToken = default)
    {
        var formData = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(fileMediaType);
        formData.Add(fileContent, "file", fileName);

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri(url))
        {
            Content = formData
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(byPassServerCertificate);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> PostFormAsync<T>(string url, Dictionary<string, string> formBody, Dictionary<string, string> header, Encoding resultEncoding, bool byPassServerSertificate, int timeOutSecond, CancellationToken cancellationToken = default)
    {
        var formData = new MultipartFormDataContent();
        if (formBody is not null)
            foreach (var item in formBody)
                formData.Add(new StringContent(item.Value, resultEncoding ?? Encoding.UTF8), item.Key);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = formData
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(byPassServerSertificate);
        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostFormAsync(string url, Dictionary<string, string> formBody, Dictionary<string, string> header, Encoding resultEncoding, bool byPassServerSertificate, int timeOutSecond, CancellationToken cancellationToken = default)
    {
        var formData = new MultipartFormDataContent();
        if (formBody is not null)
            foreach (var item in formBody)
                formData.Add(new StringContent(item.Value, resultEncoding ?? Encoding.UTF8), item.Key);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = formData
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(byPassServerSertificate);
        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }


    public static async Task<T> PostAsync<T>(HttpClient httpClient, string url, string contentJsonString, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostAsync(HttpClient httpClient, string url, string contentJsonString, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> PostAsync<T>(HttpClient httpClient, string url, string contentJsonString, Dictionary<string, string> header, Encoding resultEncoding, int timeOutSecond, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostAsync(HttpClient httpClient, string url, string contentJsonString, Dictionary<string, string> header, Encoding resultEncoding, int timeOutSecond, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostFormAsync(HttpClient httpClient, string url, Dictionary<string, string> formBody, Dictionary<string, string> header, Encoding resultEncoding, bool byPassServerSertificate, int timeOutSecond, CancellationToken cancellationToken = default)
    {
        var formData = new MultipartFormDataContent();
        if (formBody is not null)
            foreach (var item in formBody)
                formData.Add(new StringContent(item.Value, resultEncoding ?? Encoding.UTF8), item.Key);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = formData
        };
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostFormFileAsync(HttpClient httpClient, string url, byte[] fileBytes, string fileName, Dictionary<string, string> header = null, CancellationToken cancellationToken = default)
    {
        var formData = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
        formData.Add(fileContent, "file", fileName);

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri(url))
        {
            Content = formData
        };
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostFormFileAsync(HttpClient httpClient, string url, byte[] fileBytes, string fileName, string fileMediaType, Dictionary<string, string> header = null, CancellationToken cancellationToken = default)
    {
        var formData = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(fileMediaType);
        formData.Add(fileContent, "file", fileName);

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri(url))
        {
            Content = formData
        };
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }




    public static async Task<T> PutAsync<T>(string url, object contentValues, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(url))
        {
            Content = new StringContent(contentValues.SerializeToJson(), resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PutAsync(string url, object contentValues, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(url))
        {
            Content = new StringContent(contentValues.SerializeToJson(), resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> PutAsync<T>(string url, string contentJsonString, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PutAsync(string url, string contentJsonString, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> PutAsync<T>(string url, string contentJsonString, Dictionary<string, string> header, Encoding resultEncoding, bool byPassServerSertificate, int timeOutSecond, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(byPassServerSertificate);
        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PutAsync(string url, string contentJsonString, Dictionary<string, string> header, Encoding resultEncoding, bool byPassServerSertificate, int timeOutSecond, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(byPassServerSertificate);
        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PutFormAsync(string url, Dictionary<string, string> formBody, Dictionary<string, string> header = null, bool byPassServerSertificate = true, CancellationToken cancellationToken = default)
    {
        // Fixed: the request (with the headers) was built but never sent; PutAsync(url, formData) sent it without headers.
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(url));
        AddHeaders(request, header);

        var formData = new MultipartFormDataContent();
        if (formBody is not null)
            foreach (var item in formBody)
                formData.Add(new StringContent(item.Value, Encoding.UTF8), item.Key);
        request.Content = formData;

        var httpClient = SharedClient(byPassServerSertificate);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }


    public static async Task<T> PutAsync<T>(HttpClient httpClient, string url, string contentJsonString, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PutAsync(HttpClient httpClient, string url, string contentJsonString, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> PutAsync<T>(HttpClient httpClient, string url, string contentJsonString, Dictionary<string, string> header, Encoding resultEncoding, int timeOutSecond, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PutAsync(HttpClient httpClient, string url, string contentJsonString, Dictionary<string, string> header, Encoding resultEncoding, int timeOutSecond, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(url))
        {
            Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json")
        };
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> PutFormAsync(HttpClient httpClient, string url, Dictionary<string, string> formBody, Dictionary<string, string> header = null, bool byPassServerSertificate = true, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(url));
        AddHeaders(request, header);

        var formData = new MultipartFormDataContent();
        if (formBody is not null)
            foreach (var item in formBody)
                formData.Add(new StringContent(item.Value, Encoding.UTF8), item.Key);
        request.Content = formData;

        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }




    public static async Task<T> DeleteAsync<T>(string url, object contentValues = null, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(url));
        if (contentValues is not null) request.Content = new StringContent(contentValues.SerializeToJson(), resultEncoding ?? Encoding.UTF8, "application/json");
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> DeleteAsync(string url, object contentValues = null, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(url));
        if (contentValues is not null) request.Content = new StringContent(contentValues.SerializeToJson(), resultEncoding ?? Encoding.UTF8, "application/json");
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> DeleteAsync<T>(string url, string contentJsonString = null, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(url));
        if (contentJsonString is not null) request.Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json");
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> DeleteAsync(string url, string contentJsonString = null, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(url));
        if (contentJsonString is not null) request.Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json");
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> DeleteAsync<T>(string url, string contentJsonString, Dictionary<string, string> header, Encoding resultEncoding, bool byPassServerSertificate, int timeOutSecond, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(url));
        if (contentJsonString is not null) request.Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json");
        AddHeaders(request, header);

        var httpClient = SharedClient(byPassServerSertificate);
        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> DeleteAsync(string url, string contentJsonString, Dictionary<string, string> header, Encoding resultEncoding, bool byPassServerSertificate, int timeOutSecond, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(url));
        if (contentJsonString is not null) request.Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json");
        AddHeaders(request, header);

        var httpClient = SharedClient(byPassServerSertificate);
        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }


    public static async Task<T> DeleteAsync<T>(HttpClient httpClient, string url, string contentJsonString = null, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(url));
        if (contentJsonString is not null) request.Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json");
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> DeleteAsync(HttpClient httpClient, string url, string contentJsonString = null, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(url));
        if (contentJsonString is not null) request.Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json");
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> DeleteAsync<T>(HttpClient httpClient, string url, string contentJsonString, Dictionary<string, string> header, Encoding resultEncoding, int timeOutSecond, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(url));
        if (contentJsonString is not null) request.Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json");
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

    public static async Task<(HttpStatusCode httpStatusCode, string response)> DeleteAsync(HttpClient httpClient, string url, string contentJsonString, Dictionary<string, string> header, Encoding resultEncoding, int timeOutSecond, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(url));
        if (contentJsonString is not null) request.Content = new StringContent(contentJsonString, resultEncoding ?? Encoding.UTF8, "application/json");
        AddHeaders(request, header);

        using var response = await SendAsync(httpClient, request, timeOutSecond, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }




    public static async Task<(HttpStatusCode httpStatusCode, string response)> PostXMLAsync(string url, string contentXmlString, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(contentXmlString, resultEncoding ?? Encoding.UTF8, "text/xml; charset=utf-8")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public static async Task<T> PostXMLAsync<T>(string url, string contentXmlString, Dictionary<string, string> header = null, Encoding resultEncoding = null, CancellationToken cancellationToken = default) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(contentXmlString, resultEncoding ?? Encoding.UTF8, "text/xml; charset=utf-8")
        };
        AddHeaders(request, header);

        var httpClient = SharedClient(false);
        using var response = await SendAsync(httpClient, request, null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return responseBody.DeSerializeJson<T>();
    }

}