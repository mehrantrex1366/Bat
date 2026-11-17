namespace Bat.Http;

public static class ClientInfo
{
    public static string GetIP(HttpContext httpContext)
    {
        return httpContext?.Connection?.RemoteIpAddress?.ToString();
    }

    public static string GetIPBehindCloud(HttpContext httpContext)
    {
        var ip = httpContext?.Request?.Headers["CF-Connecting-IP"];
        if(!string.IsNullOrWhiteSpace(ip)) return ip;

        ip = httpContext?.Request?.Headers["True-Client-IP"];
        if (!string.IsNullOrWhiteSpace(ip)) return ip;

        return httpContext?.Connection?.RemoteIpAddress?.ToString();
    }

    public static string GetIP(IHttpContextAccessor httpContextAccessor)
    {
        try
        {
            if (httpContextAccessor != null) return httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList[2].ToString();
            if (!string.IsNullOrWhiteSpace(ip)) return ip;

            return host.AddressList.ToList().Where(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault().ToString();
        }
        catch
        {
            return "127.0.0.1";// string.Empty;
        }
    }

    /// <summary>
    /// Value for the <c>Accept-CH</c> response header. Chromium browsers then send the high-entropy client hints
    /// (real Windows 10/11, macOS and Android versions, device model, full browser version) on later HTTPS requests,
    /// and <see cref="GetRequestDetails(HttpContext)"/> uses them.
    /// </summary>
    public const string AcceptClientHints = "Sec-CH-UA-Platform-Version, Sec-CH-UA-Model, Sec-CH-UA-Full-Version-List";

    public static void GetRequestOs(string userAgent, out string os, out string version)
        => UserAgentParser.ParseOs(UserAgentParser.Normalize(userAgent), out os, out version);

    public static string GetRequestOsVersion(string userAgent, string os)
    {
        if (os == null) throw new ArgumentNullException(nameof(os));

        var index = userAgent.IndexOf(os, StringComparison.Ordinal);
        if (index < 0) return string.Empty;

        return ReadVersion(userAgent.AsSpan(index + os.Length));
    }

    public static void GetRequestBrowser(string userAgent, out string browser, out string version)
    {
        var ua = UserAgentParser.Normalize(userAgent);
        UserAgentParser.ParseOs(ua, out var os, out _);
        UserAgentParser.ParseBrowser(ua, os, out browser, out version, out _);
    }

    public static string GetRequestBrowserVersion(string userAgent, string browser)
    {
        if (browser == "Unknown") return "0.0";

        // The old code threw ArgumentOutOfRangeException when the browser name was the last thing in the
        // User-Agent (index + length + 1 > length); the exception was swallowed by GetRequestDetails.
        var index = userAgent.IndexOf(browser, StringComparison.Ordinal);
        if (index < 0) return string.Empty;

        var start = index + browser.Length + 1;
        if (start >= userAgent.Length) return string.Empty;

        return ReadVersion(userAgent.AsSpan(start));
    }

    // Reads "12.3_4" style versions (digits, '.' and '_' -> '.') from the start of the text, without per-char string allocations.
    private static string ReadVersion(ReadOnlySpan<char> text)
    {
        text = text.TrimStart();
        if (text.IsEmpty || text[0] == '.') return string.Empty;

        var length = 0;
        while (length < text.Length && (char.IsAsciiDigit(text[length]) || text[length] == '.' || text[length] == '_'))
            length++;

        return length == 0 ? string.Empty : text[..length].ToString().Replace('_', '.');
    }

    public static string GetRequestDeviceModel(string userAgent)
    {
        var ua = UserAgentParser.Normalize(userAgent);
        UserAgentParser.ParseOs(ua, out var os, out _);
        UserAgentParser.ParseDevice(ua, os, false, out _, out _, out var model);
        return model;
    }

    public static string GetRequestDeviceManufacture(string os)
    {
        string manufacture = os switch
        {
            "Unity" 
            or "Windows XP" 
            or "Windows Vista" 
            or "Windows 7" 
            or "Windows 8" 
            or "Windows 8.1" 
            or "Windows 10" 
            or "Windows 11" 
            or "Windows Phone" => $"Microsoft {os}",
            "IOS" 
            or "Mac OS" => $"Apple {os}",
            "Black Berry" => os,
            _ => "Unknown",
        };
        return manufacture;
    }

    /// <summary>Parses a User-Agent string (no client hints). Never returns <c>null</c>.</summary>
    public static RequestDetails ParseUserAgent(string userAgent) => UserAgentParser.Parse(userAgent, null);

    /// <summary>
    /// Parses a User-Agent string and refines the result with User-Agent Client Hints found in <paramref name="headers"/>
    /// (<c>Sec-CH-UA*</c>, see <see cref="AcceptClientHints"/>). Never returns <c>null</c>.
    /// </summary>
    public static RequestDetails ParseUserAgent(string userAgent, IHeaderDictionary headers) => UserAgentParser.Parse(userAgent, headers);

    public static RequestDetails GetRequestDetails(HttpContext httpContext)
    {
        try
        {
            var headers = httpContext.Request.Headers;
            var result = UserAgentParser.Parse(headers["User-Agent"].ToString(), headers);
            result.IP = GetIP(httpContext);
            return result;
        }
        catch
        {
            return null;
        }
    }

    public static DeviceLog GetDeviceLog(HttpContext httpContext)
    {
        try
        {
            var requestDetails = GetRequestDetails(httpContext);
            var ip = GetIP(httpContext);
            var isMobile = requestDetails != null && requestDetails.IsMobile;
            var os = $"{requestDetails?.OsName} {requestDetails?.OsVersion}";
            var device = $"{requestDetails?.Manufacture} {requestDetails?.Model}";
            var application = $"{requestDetails?.BrowserName} {requestDetails?.BrowserVersion}";

            return new DeviceLog
            {
                IsMobile = isMobile,
                IP = ip,
                Os = os.Length > 25 ? os[..25] : os,
                Device = device.Length > 50 ? device[..50] : device,
                Application = application.Length > 50 ? application[..50] : application
            };
        }
        catch
        {
            return null;
        }
    }

}