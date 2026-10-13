using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using DrinkWater.Core;

namespace DrinkWater.App.Persistence;

public sealed record SettingsLoadResult(ReminderPreferences Preferences, bool CanSave, string? Warning);

public sealed class SettingsStore(string path)
{
    private const int MaximumBytes = 64 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        MaxDepth = 8
    };

    public SettingsLoadResult Load()
    {
        try
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > MaximumBytes)
            {
                throw new InvalidDataException("The settings file is too large.");
            }

            StoredSettings stored = JsonSerializer.Deserialize<StoredSettings>(stream, JsonOptions)
                ?? throw new InvalidDataException("The settings file is empty.");
            if (stored.SchemaVersion != 1)
            {
                return new(new(), false, "These settings belong to another app version. They will be preserved; changes apply to this session only.");
            }

            ReminderPreferences preferences = new()
            {
                IntervalMinutes = stored.IntervalMinutes,
                RemindersEnabled = stored.RemindersEnabled,
                SoundEnabled = stored.SoundEnabled
            };
            preferences.Validate();
            return new(preferences, true, null);
        }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException)
        {
            return new(new(), true, null);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or ArgumentOutOfRangeException)
        {
            try
            {
                File.Move(path, path + ".invalid-" + Guid.NewGuid().ToString("N"));
                return new(new(), true, "Unreadable settings were backed up. Drink Water is using its defaults.");
            }
            catch (Exception backupError) when (backupError is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning("Drink Water could not back up invalid settings: {0}", backupError);
                return new(new(), false, "The settings file could not be read or backed up. Changes apply to this session only.");
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return new(new(), false, "The settings file cannot be accessed. Changes apply to this session only.");
        }
    }

    public void Save(ReminderPreferences preferences)
    {
        preferences.Validate();
        string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Settings need a directory.");
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, ".settings-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            StoredSettings stored = new(1, preferences.IntervalMinutes, preferences.RemindersEnabled, preferences.SoundEnabled);
            using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, stored, JsonOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning("Drink Water could not remove temporary settings: {0}", error);
            }
        }
    }

    private sealed record StoredSettings(
        [property: JsonRequired] int SchemaVersion,
        [property: JsonRequired] int IntervalMinutes,
        [property: JsonRequired] bool RemindersEnabled,
        [property: JsonRequired] bool SoundEnabled);
}
