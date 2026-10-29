using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using DrinkWater.Core;

namespace DrinkWater.App.Persistence;

public sealed record NetworkSettingsLoadResult(NetworkPreferences Preferences, bool CanSave, bool CanConnect, string? Warning);

public sealed class NetworkSettingsStore(string path)
{
    private const int MaximumBytes = 32 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        MaxDepth = 6,
        Converters = { new JsonStringEnumConverter<ProxyMode>(JsonNamingPolicy.CamelCase, false), new JsonStringEnumConverter<ProxyProtocol>(JsonNamingPolicy.CamelCase, false) }
    };

    public NetworkSettingsLoadResult Load()
    {
        try
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > MaximumBytes) throw new InvalidDataException();
            using JsonDocument document = JsonDocument.Parse(stream, new JsonDocumentOptions { MaxDepth = 6 });
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException();
            if (document.RootElement.TryGetProperty("schemaVersion", out JsonElement schema)
                && schema.ValueKind == JsonValueKind.Number && schema.TryGetInt32(out int version) && version != 1) return Unavailable(canSave: false);
            StoredNetworkSettings stored = document.RootElement.Deserialize<StoredNetworkSettings>(JsonOptions) ?? throw new InvalidDataException();
            if (stored.SchemaVersion != 1)
                return Unavailable(canSave: false);
            StoredProxy proxy = stored.Proxy ?? throw new InvalidDataException();
            NetworkPreferences preferences = new()
            {
                CheckForUpdatesAtStartup = stored.CheckForUpdatesAtStartup,
                Proxy = new() { Mode = proxy.Mode, Protocol = proxy.Protocol, Host = proxy.Host, Port = proxy.Port, Username = proxy.Username, ProtectedPassword = proxy.ProtectedPassword }
            };
            preferences.Validate();
            return new(preferences, true, true, null);
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
        {
            return new(new(), true, true, null);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or ArgumentException)
        {
            return Unavailable(canSave: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return Unavailable(canSave: false);
        }
    }

    public void Save(NetworkPreferences preferences)
    {
        preferences.Validate();
        string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Network settings need a directory.");
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, ".network-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            ProxySettings proxy = preferences.Proxy;
            StoredNetworkSettings stored = new(1, preferences.CheckForUpdatesAtStartup,
                new(proxy.Mode, proxy.Protocol, proxy.Host, proxy.Port, proxy.Username, proxy.ProtectedPassword));
            using (FileStream stream = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, stored, JsonOptions);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { Trace.TraceWarning("Drink Water could not remove temporary network settings."); }
        }
    }

    private static NetworkSettingsLoadResult Unavailable(bool canSave) =>
        new(new() { CheckForUpdatesAtStartup = false }, canSave, false,
            "Network settings could not be loaded. Update checks are paused. Choose a mode under Proxy to enable them; your saved file is preserved until you apply settings.");

    private sealed record StoredNetworkSettings(
        [property: JsonRequired] int SchemaVersion,
        [property: JsonRequired] bool CheckForUpdatesAtStartup,
        [property: JsonRequired] StoredProxy Proxy);

    private sealed record StoredProxy(
        [property: JsonRequired] ProxyMode Mode,
        [property: JsonRequired] ProxyProtocol Protocol,
        [property: JsonRequired] string Host,
        [property: JsonRequired] int Port,
        [property: JsonRequired] string Username,
        [property: JsonRequired] string? ProtectedPassword);
}
