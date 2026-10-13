using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DrinkWater.App.WindowsIntegration;

public sealed partial class WaterSound(string soundPath) : IDisposable
{
    private const string Alias = "DrinkWaterReminderAudio";
    private readonly System.Windows.Forms.Timer _stopTimer = new() { Interval = 5000 };
    private bool _opened;

    public event EventHandler<Exception>? PlaybackFailed;

    public void Play()
    {
        Stop();
        if (!File.Exists(soundPath) || soundPath.Contains('"'))
        {
            throw new FileNotFoundException("The bundled water sound is unavailable.", soundPath);
        }

        Check(MciSendString($"open \"{soundPath}\" type mpegvideo alias {Alias}", 0, 0, 0));
        _opened = true;
        try
        {
            Check(MciSendString($"play {Alias} from 0", 0, 0, 0));
            _stopTimer.Tick -= StopAfterPlayback;
            _stopTimer.Tick += StopAfterPlayback;
            _stopTimer.Start();
        }
        catch
        {
            try
            {
                Stop();
            }
            catch (Win32Exception error)
            {
                PlaybackFailed?.Invoke(this, error);
            }

            throw;
        }
    }

    public void Stop()
    {
        _stopTimer.Stop();
        if (_opened)
        {
            uint result = MciSendString($"close {Alias}", 0, 0, 0);
            Check(result);
            _opened = false;
        }
    }

    public void Dispose()
    {
        try
        {
            Stop();
        }
        finally
        {
            _stopTimer.Dispose();
        }
    }

    private void StopAfterPlayback(object? sender, EventArgs args)
    {
        try
        {
            Stop();
        }
        catch (Win32Exception error)
        {
            PlaybackFailed?.Invoke(this, error);
        }
    }

    private static void Check(uint result)
    {
        if (result != 0)
        {
            throw new Win32Exception((int)result, $"Windows could not play or close the water sound (MCI error {result}).");
        }
    }

    [LibraryImport("winmm.dll", EntryPoint = "mciSendStringW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint MciSendString(string command, nint returnBuffer, uint returnLength, nint callback);
}
