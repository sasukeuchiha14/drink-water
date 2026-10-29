using System.Net;
using System.Net.Http;
using DrinkWater.App.Persistence;
using DrinkWater.Core;

namespace DrinkWater.App.Updates;

public static class ProxyClientFactory
{
    public static HttpClient Create(ProxySettings settings) => new(CreateHandler(settings)) { Timeout = Timeout.InfiniteTimeSpan };

    public static HttpClientHandler CreateHandler(ProxySettings settings)
    {
        settings.Validate();
        WebProxy? proxy = null;
        if (settings.Mode == ProxyMode.Custom)
        {
            proxy = new(settings.GetAddress()) { BypassProxyOnLocal = false };
            if (settings.Username.Length > 0)
                proxy.Credentials = new NetworkCredential(settings.Username, ProxyPasswordProtection.Unprotect(settings));
        }
        return new() { AllowAutoRedirect = false, UseProxy = settings.Mode != ProxyMode.Direct, Proxy = proxy };
    }
}
