using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using DrinkWater.App.Updates;
using DrinkWater.Core;

namespace DrinkWater.Tests;

public sealed class UpdateCheckerTests
{
    [Theory]
    [InlineData("1.10.0", "1.9.9", 1)]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("2.0.0", "1.99.99", 1)]
    [InlineData("1.1.0", "1.1.0", 0)]
    public void ComparesStableVersionsNumerically(string left, string right, int expected)
    {
        Assert.True(ReleaseVersion.TryParse(left, out ReleaseVersion a));
        Assert.True(ReleaseVersion.TryParse(right, out ReleaseVersion b));
        Assert.Equal(expected, Math.Sign(a.CompareTo(b)));
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1.0.0.0")]
    [InlineData("v1.0.0")]
    [InlineData("01.0.0")]
    [InlineData("1.0.0-beta")]
    [InlineData("2147483648.0.0")]
    [InlineData("1.0.0\n")]
    [InlineData("-1.0.0")]
    [InlineData("")]
    [InlineData(null)]
    public void RejectsUnsupportedVersions(string? value) => Assert.False(ReleaseVersion.TryParse(value, out _));

    [Theory]
    [InlineData("1.10.0", true)]
    [InlineData("1.9.0", false)]
    [InlineData("1.8.0", false)]
    public async Task DetectsUpdatesWithoutOfferingADowngrade(string latest, bool expected)
    {
        using HttpClient client = new(new FakeHandler((_, _) => Task.FromResult(Manifest(latest))));
        UpdateCheckResult result = await new UpdateChecker(client).CheckAsync(new(1, 9, 0));
        Assert.Equal(expected, result.UpdateAvailable);
        Assert.Equal($"https://github.com/sasukeuchiha14/drink-water/releases/download/v{latest}/DrinkWater-Setup.exe", result.DownloadUrl.AbsoluteUri);
    }

    [Fact]
    public async Task UsesSecondDomainWhenFirstIsUnavailable()
    {
        List<string> hosts = [];
        using HttpClient client = new(new FakeHandler((request, _) =>
        {
            hosts.Add(request.RequestUri!.Host);
            Assert.Equal("?version=1.9.0", request.RequestUri.Query);
            return Task.FromResult(hosts.Count == 1 ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : Manifest("1.10.0"));
        }));
        Assert.True((await new UpdateChecker(client).CheckAsync(new(1, 9, 0))).UpdateAvailable);
        Assert.Equal(new[] { "drinkwater.hardikgarg.me", "drink-water.hardikgarg.me" }, hosts);
    }

    [Theory]
    [InlineData("{}", "application/json")]
    [InlineData("not json", "application/json")]
    [InlineData("<html>Redirect</html>", "text/html")]
    [InlineData("{\"schemaVersion\":2}", "application/json")]
    public async Task RejectsInvalidManifests(string body, string mediaType)
    {
        int calls = 0;
        using HttpClient client = new(new FakeHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, mediaType) });
        }));
        await Assert.ThrowsAsync<HttpRequestException>(() => new UpdateChecker(client).CheckAsync(new(1, 9, 0)));
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData("https://evil.example/DrinkWater-Setup.exe")]
    [InlineData("http://github.com/sasukeuchiha14/drink-water/releases/download/v1.10.0/DrinkWater-Setup.exe")]
    [InlineData("https://github.com/other/repository/releases/download/v1.10.0/DrinkWater-Setup.exe")]
    public async Task RejectsUntrustedDownloadDestinations(string download)
    {
        using HttpClient client = new(new FakeHandler((_, _) => Task.FromResult(Manifest("1.10.0", download))));
        await Assert.ThrowsAsync<HttpRequestException>(() => new UpdateChecker(client).CheckAsync(new(1, 9, 0)));
    }

    [Fact]
    public async Task BoundsResponseSizeEvenWithoutContentLength()
    {
        using HttpClient client = new(new FakeHandler((_, _) =>
        {
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StreamContent(new NonSeekableStream(new byte[17000]))
            };
            response.Content.Headers.ContentType = new("application/json");
            Assert.Null(response.Content.Headers.ContentLength);
            return Task.FromResult(response);
        }));
        await Assert.ThrowsAsync<HttpRequestException>(() => new UpdateChecker(client).CheckAsync(new(1, 9, 0)));
    }

    [Fact]
    public async Task CancellationStopsTheCheckWithoutTryingTheFallback()
    {
        int calls = 0;
        using CancellationTokenSource cancellation = new();
        using HttpClient client = new(new FakeHandler(async (_, token) =>
        {
            calls++;
            cancellation.Cancel();
            await Task.Delay(Timeout.Infinite, token);
            return Manifest("1.10.0");
        }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new UpdateChecker(client).CheckAsync(new(1, 9, 0), cancellation.Token));
        Assert.Equal(1, calls);
    }

    private static HttpResponseMessage Manifest(string latest, string? download = null) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            currentVersion = "1.9.0",
            latestVersion = latest,
            releaseUrl = $"https://github.com/sasukeuchiha14/drink-water/releases/tag/v{latest}",
            downloadUrl = download ?? $"https://github.com/sasukeuchiha14/drink-water/releases/download/v{latest}/DrinkWater-Setup.exe"
        }), Encoding.UTF8, "application/json")
    };

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request, cancellationToken);
    }

    private sealed class NonSeekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }
}
