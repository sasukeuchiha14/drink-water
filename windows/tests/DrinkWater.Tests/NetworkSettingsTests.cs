using System.Security.Cryptography;
using DrinkWater.App.Persistence;
using DrinkWater.Core;

namespace DrinkWater.Tests;

public sealed class NetworkSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DrinkWater.NetworkTests", Guid.NewGuid().ToString("N"));
    private readonly string _path;
    private readonly NetworkSettingsStore _store;

    public NetworkSettingsTests()
    {
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "network.json");
        _store = new(_path);
    }

    [Fact]
    public void MissingNetworkSettingsEnableStartupChecksUsingTheSystemProxy()
    {
        NetworkSettingsLoadResult result = _store.Load();
        Assert.True(result.CanConnect);
        Assert.True(result.CanSave);
        Assert.True(result.Preferences.CheckForUpdatesAtStartup);
        Assert.Equal(ProxyMode.System, result.Preferences.Proxy.Mode);
        Assert.False(File.Exists(_path));
    }

    [Fact]
    public void NetworkChoicesRoundTripWithoutStoringAPlaintextPassword()
    {
        const string password = "Synthetic-test-password-水";
        ProxySettings proxy = new() { Mode = ProxyMode.Custom, Protocol = ProxyProtocol.Socks5, Host = "proxy.example", Port = 1080, Username = "test-user" };
        proxy = proxy with { ProtectedPassword = ProxyPasswordProtection.Protect(password, proxy) };
        NetworkPreferences preferences = new() { CheckForUpdatesAtStartup = false, Proxy = proxy };
        _store.Save(preferences);
        Assert.DoesNotContain(password, File.ReadAllText(_path));
        Assert.Equal(preferences, _store.Load().Preferences);
        Assert.Equal(password, ProxyPasswordProtection.Unprotect(_store.Load().Preferences.Proxy));
        _store.Save(preferences with { CheckForUpdatesAtStartup = true, Proxy = proxy with { Mode = ProxyMode.Direct } });
        Assert.Equal(ProxyMode.Direct, _store.Load().Preferences.Proxy.Mode);
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("""{"schemaVersion":"invalid"}""")]
    [InlineData("""{"schemaVersion":null}""")]
    [InlineData("""{"schemaVersion":1,"checkForUpdatesAtStartup":true,"proxy":null}""")]
    [InlineData("""{"schemaVersion":1,"checkForUpdatesAtStartup":true,"proxy":{"mode":"custom"}}""")]
    public void InvalidNetworkSettingsPauseConnectionsAndPreserveTheFile(string contents)
    {
        File.WriteAllText(_path, contents);
        NetworkSettingsLoadResult result = _store.Load();
        Assert.False(result.CanConnect);
        Assert.False(result.Preferences.CheckForUpdatesAtStartup);
        Assert.NotNull(result.Warning);
        Assert.True(result.CanSave);
        Assert.Equal(contents, File.ReadAllText(_path));
    }

    [Fact]
    public void UnknownAndLockedNetworkSettingsArePreservedAndNotMarkedWritable()
    {
        File.WriteAllText(_path, """{"schemaVersion":99}""");
        Assert.False(_store.Load().CanSave);
        Assert.False(_store.Load().CanConnect);
        _store.Save(new());
        string future = File.ReadAllText(_path).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 99", StringComparison.Ordinal);
        File.WriteAllText(_path, future);
        Assert.False(_store.Load().CanSave);
        Assert.False(_store.Load().CanConnect);
        Assert.Equal(future, File.ReadAllText(_path));
        using FileStream locked = new(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.False(_store.Load().CanSave);
        Assert.False(_store.Load().CanConnect);
    }

    [Fact]
    public void OversizedAndInvalidNetworkSettingsCannotSilentlyEnableDirectAccess()
    {
        File.WriteAllText(_path, new string('x', 32769));
        Assert.False(_store.Load().CanConnect);
        _store.Save(new());
        string contents = File.ReadAllText(_path).Replace("\"system\"", "\"unsupported\"", StringComparison.Ordinal);
        File.WriteAllText(_path, contents);
        Assert.False(_store.Load().CanConnect);
        Assert.Equal(contents, File.ReadAllText(_path));
        Assert.Throws<ArgumentException>(() => _store.Save(new() { Proxy = new() { Host = "http://proxy.example" } }));
        Assert.Equal(contents, File.ReadAllText(_path));
    }

    [Fact]
    public void PasswordProtectionIsBoundToTheProxyAndUsername()
    {
        ProxySettings proxy = new() { Username = "test-user" };
        proxy = proxy with { ProtectedPassword = ProxyPasswordProtection.Protect("synthetic-password", proxy) };
        Assert.Equal("synthetic-password", ProxyPasswordProtection.Unprotect(proxy));
        Assert.Throws<CryptographicException>(() => ProxyPasswordProtection.Unprotect(proxy with { Host = "different.example" }));
        Assert.Throws<CryptographicException>(() => ProxyPasswordProtection.Unprotect(proxy with { Username = "someone-else" }));
        Assert.Throws<CryptographicException>(() => ProxyPasswordProtection.Unprotect(proxy with { Protocol = ProxyProtocol.Https }));
        Assert.Throws<FormatException>(() => ProxyPasswordProtection.Unprotect(proxy with { ProtectedPassword = "not base64" }));
        Assert.Throws<ArgumentException>(() => ProxyPasswordProtection.Protect(new string('水', 86), proxy with { Protocol = ProxyProtocol.Socks5, ProtectedPassword = null }));
    }

    public void Dispose()
    {
        string allowed = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DrinkWater.NetworkTests")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(_directory).StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
        Directory.Delete(_directory, recursive: true);
    }
}
