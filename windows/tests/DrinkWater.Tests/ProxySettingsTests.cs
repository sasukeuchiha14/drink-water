using System.Net;
using DrinkWater.App.Persistence;
using DrinkWater.App.Updates;
using DrinkWater.Core;

namespace DrinkWater.Tests;

public sealed class ProxySettingsTests
{
    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("proxy.example")]
    public void ExplicitProxiesSupportAllRequestedProtocolsAndHostTypes(string host)
    {
        foreach (ProxyProtocol protocol in Enum.GetValues<ProxyProtocol>())
        {
            ProxySettings settings = new() { Mode = ProxyMode.Custom, Protocol = protocol, Host = host, Port = 1234 };
            using var handler = ProxyClientFactory.CreateHandler(settings);
            WebProxy proxy = Assert.IsType<WebProxy>(handler.Proxy);
            Assert.Equal(protocol.ToString().ToLowerInvariant(), proxy.Address!.Scheme);
            Assert.Equal(1234, proxy.Address.Port);
            Assert.True(handler.UseProxy);
            Assert.False(handler.AllowAutoRedirect);
            Assert.False(proxy.BypassProxyOnLocal);
            Assert.False(proxy.IsBypassed(new Uri("https://updates.example/")));
            Assert.Equal("", proxy.Address.UserInfo);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("http://proxy.example")]
    [InlineData("user:password@proxy.example")]
    [InlineData("proxy.example:8080")]
    [InlineData("proxy.example/path")]
    [InlineData("proxy.example?x=1")]
    [InlineData("proxy example")]
    public void ProxyHostsRejectEmbeddedUrlsCredentialsAndPaths(string host) =>
        Assert.Throws<ArgumentException>(() => new ProxySettings { Mode = ProxyMode.Custom, Host = host }.Validate());

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    [InlineData(-1)]
    public void InvalidPortsAreRejected(int port) =>
        Assert.Throws<ArgumentException>(() => new ProxySettings { Port = port }.Validate());

    [Fact]
    public void DirectAndSystemModesDoNotOverrideGlobalProxySettings()
    {
        using var direct = ProxyClientFactory.CreateHandler(new() { Mode = ProxyMode.Direct });
        using var system = ProxyClientFactory.CreateHandler(new());
        Assert.False(direct.UseProxy);
        Assert.Null(direct.Proxy);
        Assert.True(system.UseProxy);
        Assert.Null(system.Proxy);
        Assert.Null(system.Credentials);
    }

    [Fact]
    public void CredentialsBelongToTheProxyAndNeverToOriginRequests()
    {
        ProxySettings settings = new() { Mode = ProxyMode.Custom, Username = "synthetic-user" };
        settings = settings with { ProtectedPassword = ProxyPasswordProtection.Protect("synthetic-password", settings) };
        using var handler = ProxyClientFactory.CreateHandler(settings);
        WebProxy proxy = Assert.IsType<WebProxy>(handler.Proxy);
        NetworkCredential credential = Assert.IsType<NetworkCredential>(proxy.Credentials);
        Assert.Equal("synthetic-user", credential.UserName);
        Assert.Equal("synthetic-password", credential.Password);
        Assert.Null(handler.Credentials);
        Assert.False(handler.UseDefaultCredentials);
        Assert.Throws<FormatException>(() => ProxyClientFactory.CreateHandler(settings with { ProtectedPassword = "not base64" }));
    }
}
