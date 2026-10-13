using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using DrinkWater.Core;

namespace DrinkWater.App.Updates;

public sealed record UpdateCheckResult(ReleaseVersion CurrentVersion, ReleaseVersion LatestVersion, Uri ReleaseUrl, Uri DownloadUrl)
{
    public bool UpdateAvailable => LatestVersion.CompareTo(CurrentVersion) > 0;
}

public sealed class UpdateChecker(HttpClient client)
{
    private const int MaximumResponseBytes = 16 * 1024;
    private const string RepositoryUrl = "https://github.com/sasukeuchiha14/drink-water";
    private static readonly string[] Endpoints =
    [
        "https://drinkwater.hardikgarg.me/api/update",
        "https://drink-water.hardikgarg.me/api/update"
    ];

    public async Task<UpdateCheckResult> CheckAsync(ReleaseVersion currentVersion, CancellationToken cancellationToken = default)
    {
        List<Exception> failures = [];
        foreach (string endpoint in Endpoints)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            try
            {
                using HttpRequestMessage request = new(HttpMethod.Get, $"{endpoint}?version={currentVersion}");
                request.Headers.Accept.ParseAdd("application/json");
                request.Headers.UserAgent.ParseAdd($"DrinkWater/{currentVersion}");
                using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentType?.MediaType != "application/json"
                    || response.Content.Headers.ContentLength > MaximumResponseBytes)
                {
                    throw new InvalidDataException("The update endpoint returned an unsupported response.");
                }

                await using Stream stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                using MemoryStream body = new();
                byte[] buffer = new byte[4096];
                int count;
                while ((count = await stream.ReadAsync(buffer, timeout.Token).ConfigureAwait(false)) > 0)
                {
                    if (body.Length + count > MaximumResponseBytes)
                    {
                        throw new InvalidDataException("The update response exceeded the size limit.");
                    }

                    body.Write(buffer, 0, count);
                }

                UpdateManifest manifest = JsonSerializer.Deserialize<UpdateManifest>(body.ToArray())
                    ?? throw new InvalidDataException("The update response was empty.");
                if (manifest.SchemaVersion != 1 || manifest.CurrentVersion != currentVersion.ToString()
                    || !ReleaseVersion.TryParse(manifest.LatestVersion, out ReleaseVersion latest))
                {
                    throw new InvalidDataException("The update response contained an unsupported version.");
                }

                // Construct trusted destinations locally; never open arbitrary URLs from the network.
                Uri releaseUrl = new($"{RepositoryUrl}/releases/tag/v{latest}");
                Uri downloadUrl = new($"{RepositoryUrl}/releases/download/v{latest}/DrinkWater-Setup.exe");
                if (manifest.ReleaseUrl != releaseUrl.AbsoluteUri || manifest.DownloadUrl != downloadUrl.AbsoluteUri)
                {
                    throw new InvalidDataException("The update response contained an untrusted download address.");
                }

                return new(currentVersion, latest, releaseUrl, downloadUrl);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error) when (error is HttpRequestException or IOException or InvalidDataException or JsonException or OperationCanceledException)
            {
                failures.Add(error);
            }
        }

        throw new HttpRequestException("Neither update endpoint was available.", new AggregateException(failures));
    }

    private sealed record UpdateManifest
    {
        [JsonRequired, JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; init; }

        [JsonRequired, JsonPropertyName("currentVersion")]
        public string? CurrentVersion { get; init; }

        [JsonRequired, JsonPropertyName("latestVersion")]
        public string? LatestVersion { get; init; }

        [JsonRequired, JsonPropertyName("releaseUrl")]
        public string? ReleaseUrl { get; init; }

        [JsonRequired, JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; init; }
    }
}
