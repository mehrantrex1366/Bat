using System.Net;
using Bat.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Bat.Regression.Tests;

public sealed class LocalServer : IAsyncLifetime
{
    public WebApplication App { get; private set; }
    public string Url { get; private set; }

    public async Task InitializeAsync()
    {
        App = WebApplication.CreateBuilder().Build();
        App.Urls.Add("http://127.0.0.1:0");
        App.MapGet("/json", (HttpRequest req) => Results.Json(new
        {
            h = req.Headers["X-T"].ToString(),
            accept = req.Headers.Accept.ToString(),
            cookie = req.Headers.Cookie.ToString()
        }));
        App.MapGet("/slow", async () => { await Task.Delay(3000); return "ok"; });
        App.MapGet("/setcookie", (HttpResponse res) => { res.Cookies.Append("c", "1"); return "ok"; });
        App.MapPut("/form", async (HttpRequest req) => { var f = await req.ReadFormAsync(); return $"{req.Headers["X-T"]}|{f["a"]}"; });
        await App.StartAsync();
        Url = App.Urls.First();
    }

    public async Task DisposeAsync() => await App.DisposeAsync();
}

public class HttpRequestToolsTests(LocalServer server) : IClassFixture<LocalServer>
{
    private static readonly Dictionary<string, string> None = [];

    [Fact]
    public async Task Headers_AreSent_AndNotLeakedBetweenCalls()
    {
        var withHeader = await HttpRequestTools.GetAsync<Dictionary<string, string>>($"{server.Url}/json", None, new Dictionary<string, string> { ["X-T"] = "v" });
        Assert.Equal("v", withHeader["h"]);

        var accept = await HttpRequestTools.GetAsync<Dictionary<string, string>>($"{server.Url}/json", "application/xml");
        Assert.Equal("application/xml", accept["accept"]);

        var plain = await HttpRequestTools.GetAsync<Dictionary<string, string>>($"{server.Url}/json", None, None);
        Assert.Equal("", plain["accept"]);
        Assert.Equal("", plain["h"]);
    }

    [Fact]
    public async Task Cookies_AreNotSharedBetweenCalls()
    {
        await HttpRequestTools.GetAsync($"{server.Url}/setcookie");
        var result = await HttpRequestTools.GetAsync<Dictionary<string, string>>($"{server.Url}/json", None, None);
        Assert.Equal("", result["cookie"]);
    }

    [Fact]
    public async Task Timeout_IsPerRequest()
    {
        var ex = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            HttpRequestTools.GetAsync<string>($"{server.Url}/slow", None, None, false, 1));
        Assert.IsType<TimeoutException>(ex.InnerException);
    }

    [Fact]
    public async Task CallerClient_CanBeReusedWithTimeout()
    {
        using var client = new HttpClient();
        await HttpRequestTools.GetAsync(client, $"{server.Url}/json", None, None, 5);
        var second = await HttpRequestTools.GetAsync(client, $"{server.Url}/json", None, None, 5);
        Assert.Equal(HttpStatusCode.OK, second.httpStatusCode);
    }

    [Fact]
    public async Task PutForm_SendsHeadersAndBody()
    {
        var result = await HttpRequestTools.PutFormAsync($"{server.Url}/form", new Dictionary<string, string> { ["a"] = "1" }, new Dictionary<string, string> { ["X-T"] = "hdr" }, false);
        Assert.Equal("hdr|1", result.response);
    }

    [Fact]
    public void ClientInfo_HandlesUserAgentsWithoutParentheses()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.UserAgent = "kube-probe/1.29";
        Assert.NotNull(ClientInfo.GetRequestDetails(ctx));

        ctx.Request.Headers.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.6099.71 Safari/537.36";
        var details = ClientInfo.GetRequestDetails(ctx);
        Assert.Equal("Chrome", details.BrowserName);
        Assert.Equal("120.0.6099.71", details.BrowserVersion);
        Assert.Equal("Windows 10", details.OsName);
    }
}
