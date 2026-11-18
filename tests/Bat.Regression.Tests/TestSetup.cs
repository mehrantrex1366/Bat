using System.Net;
using System.Runtime.CompilerServices;

namespace Bat.Regression.Tests;

internal static class TestSetup
{
    // The HTTP and Swagger tests call a server on 127.0.0.1. When the machine has HTTP_PROXY/HTTPS_PROXY set
    // (common on developer machines), loopback requests would go to the proxy and fail with empty responses.
    // Runs before any test, i.e. before Bat.Http's shared handler reads HttpClient.DefaultProxy.
    [ModuleInitializer]
    internal static void BypassProxyForLoopback()
        => HttpClient.DefaultProxy = new LoopbackBypassProxy(HttpClient.DefaultProxy);

    private sealed class LoopbackBypassProxy(IWebProxy inner) : IWebProxy
    {
        public ICredentials Credentials { get => inner.Credentials; set => inner.Credentials = value; }
        public Uri GetProxy(Uri destination) => inner.GetProxy(destination);
        public bool IsBypassed(Uri host) => host.IsLoopback || inner.IsBypassed(host);
    }
}
