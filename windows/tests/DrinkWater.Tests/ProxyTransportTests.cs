using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DrinkWater.App.Persistence;
using DrinkWater.App.Updates;
using DrinkWater.Core;

namespace DrinkWater.Tests;

public sealed class ProxyTransportTests
{
    [Theory]
    [InlineData(ProxyProtocol.Http, false)]
    [InlineData(ProxyProtocol.Https, false)]
    [InlineData(ProxyProtocol.Socks5, false)]
    [InlineData(ProxyProtocol.Socks5, true)]
    public async Task HttpsRequestsUseTheSelectedProxyTransport(ProxyProtocol protocol, bool authenticate)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(15));
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        SubjectAlternativeNameBuilder names = new();
        names.AddDnsName("localhost");
        names.AddDnsName("updates.invalid");
        names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
        using X509Certificate2 generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        // Windows SChannel needs a key container rather than an ephemeral RSA key.
        using X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.UserKeySet);
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        ProxySettings settings = new() { Mode = ProxyMode.Custom, Protocol = protocol, Port = port, Username = authenticate ? "test-user" : "" };
        if (authenticate) settings = settings with { ProtectedPassword = ProxyPasswordProtection.Protect("test-password", settings) };
        using HttpClientHandler handler = ProxyClientFactory.CreateHandler(settings);
        // Trust only this test certificate, without changing machine trust or production TLS validation.
        handler.ServerCertificateCustomValidationCallback = (_, presented, _, _) => presented?.Thumbprint == certificate.Thumbprint;
        using HttpClient client = new(handler) { Timeout = TimeSpan.FromSeconds(10) };
        Task server = ServeAsync(listener, protocol, authenticate, certificate, timeout.Token);
        Task<string> response = client.GetStringAsync("https://updates.invalid/through-proxy", timeout.Token);
        await Task.WhenAll(server, response);
        Assert.Equal("through-proxy", await response);
    }

    [Fact]
    public async Task UnavailableCustomProxyFailsWithoutContactingTheOriginDirectly()
    {
        using TcpListener origin = new(IPAddress.Loopback, 0);
        origin.Start();
        using TcpListener unused = new(IPAddress.Loopback, 0);
        unused.Start();
        int unavailablePort = ((IPEndPoint)unused.LocalEndpoint).Port;
        unused.Stop();
        using HttpClient client = ProxyClientFactory.Create(new() { Mode = ProxyMode.Custom, Port = unavailablePort });
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        int originPort = ((IPEndPoint)origin.LocalEndpoint).Port;
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"http://127.0.0.1:{originPort}/", timeout.Token));
        Assert.False(origin.Pending());
    }

    private static async Task ServeAsync(TcpListener listener, ProxyProtocol protocol, bool authenticate, X509Certificate2 certificate, CancellationToken token)
    {
        using TcpClient socket = await listener.AcceptTcpClientAsync(token);
        using NetworkStream network = socket.GetStream();
        SslStream? encryptedProxy = null;
        try
        {
            Stream tunnel = network;
            if (protocol == ProxyProtocol.Https)
            {
                encryptedProxy = new(network, leaveInnerStreamOpen: true);
                await AuthenticateAsync(encryptedProxy, certificate, token);
                tunnel = encryptedProxy;
            }
            if (protocol == ProxyProtocol.Socks5)
            {
                byte[] greeting = await ReadAsync(tunnel, 2, token);
                Assert.Equal(5, greeting[0]);
                byte[] methods = await ReadAsync(tunnel, greeting[1], token);
                byte method = authenticate ? (byte)2 : (byte)0;
                Assert.Contains(method, methods);
                await tunnel.WriteAsync(new byte[] { 5, method }, token);
                if (authenticate)
                {
                    byte[] auth = await ReadAsync(tunnel, 2, token);
                    Assert.Equal(1, auth[0]);
                    Assert.Equal("test-user", Encoding.UTF8.GetString(await ReadAsync(tunnel, auth[1], token)));
                    int length = (await ReadAsync(tunnel, 1, token))[0];
                    Assert.Equal("test-password", Encoding.UTF8.GetString(await ReadAsync(tunnel, length, token)));
                    await tunnel.WriteAsync(new byte[] { 1, 0 }, token);
                }
                byte[] connect = await ReadAsync(tunnel, 4, token);
                Assert.Equal(new byte[] { 5, 1, 0, 3 }, connect);
                int hostLength = (await ReadAsync(tunnel, 1, token))[0];
                Assert.Equal("updates.invalid", Encoding.ASCII.GetString(await ReadAsync(tunnel, hostLength, token)));
                Assert.Equal(new byte[] { 1, 187 }, await ReadAsync(tunnel, 2, token));
                await tunnel.WriteAsync(new byte[] { 5, 0, 0, 1, 127, 0, 0, 1, 0, 0 }, token);
            }
            else
            {
                string connect = await ReadHeadersAsync(tunnel, token);
                Assert.StartsWith("CONNECT updates.invalid:443 HTTP/1.1\r\n", connect);
                await tunnel.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n"), token);
            }
            using SslStream destination = new(tunnel, leaveInnerStreamOpen: true);
            await AuthenticateAsync(destination, certificate, token);
            string headers = await ReadHeadersAsync(destination, token);
            Assert.StartsWith("GET /through-proxy HTTP/1.1\r\n", headers);
            Assert.DoesNotContain("Proxy-Authorization", headers, StringComparison.OrdinalIgnoreCase);
            byte[] body = Encoding.ASCII.GetBytes("through-proxy");
            await destination.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n"), token);
            await destination.WriteAsync(body, token);
        }
        finally
        {
            if (encryptedProxy is not null) await encryptedProxy.DisposeAsync();
        }
    }

    private static Task AuthenticateAsync(SslStream stream, X509Certificate2 certificate, CancellationToken token) =>
        stream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = certificate }, token);

    private static async Task<byte[]> ReadAsync(Stream stream, int count, CancellationToken token)
    {
        byte[] bytes = new byte[count];
        await stream.ReadExactlyAsync(bytes, token);
        return bytes;
    }

    private static async Task<string> ReadHeadersAsync(Stream stream, CancellationToken token)
    {
        using MemoryStream bytes = new();
        while (bytes.Length < 8192)
        {
            byte[] next = await ReadAsync(stream, 1, token);
            bytes.WriteByte(next[0]);
            if (bytes.Length >= 4)
            {
                string text = Encoding.ASCII.GetString(bytes.GetBuffer(), 0, (int)bytes.Length);
                if (text.EndsWith("\r\n\r\n", StringComparison.Ordinal)) return text;
            }
        }
        throw new InvalidDataException("Test proxy request was too large.");
    }
}
