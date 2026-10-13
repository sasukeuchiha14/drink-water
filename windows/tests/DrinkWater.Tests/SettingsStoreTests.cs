using DrinkWater.App.Persistence;
using DrinkWater.Core;

namespace DrinkWater.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DrinkWater.Tests", Guid.NewGuid().ToString("N"));
    private readonly string _path;
    private readonly SettingsStore _store;

    public SettingsStoreTests()
    {
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "settings.json");
        _store = new(_path);
    }

    [Fact]
    public void MissingSettingsUseTheDefaults()
    {
        SettingsLoadResult loaded = _store.Load();
        Assert.Equal(new ReminderPreferences(), loaded.Preferences);
        Assert.True(loaded.CanSave);
        Assert.Null(loaded.Warning);
    }

    [Theory]
    [InlineData(10, true, false)]
    [InlineData(30, false, true)]
    [InlineData(60, true, true)]
    public void SavedPreferencesRoundTrip(int minutes, bool enabled, bool sound)
    {
        ReminderPreferences preferences = new() { IntervalMinutes = minutes, RemindersEnabled = enabled, SoundEnabled = sound };
        _store.Save(preferences);
        SettingsLoadResult loaded = _store.Load();
        Assert.Equal(preferences, loaded.Preferences);
        Assert.Null(loaded.Warning);
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public void SavingReplacesAnExistingFileWithoutLeavingTemporaryFiles()
    {
        _store.Save(new());
        _store.Save(new() { IntervalMinutes = 60, RemindersEnabled = false });
        Assert.Equal(60, _store.Load().Preferences.IntervalMinutes);
        Assert.False(_store.Load().Preferences.RemindersEnabled);
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{"schemaVersion":1,"intervalMinutes":30}""")]
    [InlineData("""{"schemaVersion":1,"intervalMinutes":15,"remindersEnabled":true,"soundEnabled":true}""")]
    public void CorruptSettingsAreBackedUpBeforeDefaultsAreUsed(string contents)
    {
        File.WriteAllText(_path, contents);
        SettingsLoadResult loaded = _store.Load();
        Assert.Equal(new ReminderPreferences(), loaded.Preferences);
        Assert.True(loaded.CanSave);
        Assert.NotNull(loaded.Warning);
        string backup = Assert.Single(Directory.GetFiles(_directory, "settings.json.invalid-*"));
        Assert.Equal(contents, File.ReadAllText(backup));
    }

    [Fact]
    public void NewerSettingsArePreservedAndNotMarkedWritable()
    {
        const string contents = """{"schemaVersion":2,"intervalMinutes":60,"remindersEnabled":false,"soundEnabled":false}""";
        File.WriteAllText(_path, contents);
        SettingsLoadResult loaded = _store.Load();
        Assert.False(loaded.CanSave);
        Assert.NotNull(loaded.Warning);
        Assert.Equal(contents, File.ReadAllText(_path));
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public void OversizedSettingsAreRejectedAndBackedUp()
    {
        File.WriteAllText(_path, new string('x', 65537));
        Assert.NotNull(_store.Load().Warning);
        Assert.Single(Directory.GetFiles(_directory, "settings.json.invalid-*"));
    }

    [Fact]
    public void LockedSettingsAreNotOverwritten()
    {
        _store.Save(new());
        using (FileStream locked = new(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.False(_store.Load().CanSave);
            Exception? error = Record.Exception(() => _store.Save(new() { IntervalMinutes = 10 }));
            Assert.True(error is IOException or UnauthorizedAccessException);
        }

        Assert.Equal(30, _store.Load().Preferences.IntervalMinutes);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void InvalidPreferencesDoNotOverwriteValidSettings()
    {
        _store.Save(new());
        Assert.Throws<ArgumentOutOfRangeException>(() => _store.Save(new() { IntervalMinutes = -1 }));
        Assert.Equal(30, _store.Load().Preferences.IntervalMinutes);
    }

    public void Dispose()
    {
        string allowedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DrinkWater.Tests")) + Path.DirectorySeparatorChar;
        string target = Path.GetFullPath(_directory);
        if (!target.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refusing to remove files outside the test directory.");
        }

        Directory.Delete(target, recursive: true);
    }
}
