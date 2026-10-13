using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace DrinkWater.App.WindowsIntegration;

public sealed class ShutdownSignal : IDisposable
{
    private readonly EventWaitHandle _requested = new(false, EventResetMode.AutoReset, Name("Quit"));
    private readonly EventWaitHandle _completed = new(false, EventResetMode.ManualReset, Name("Exited"));

    public ShutdownSignal() => _completed.Reset();

    public bool IsRequested => _requested.WaitOne(0);

    public void Dispose()
    {
        _completed.Set();
        _completed.Dispose();
        _requested.Dispose();
    }

    public static bool RequestExit()
    {
        if (!EventWaitHandle.TryOpenExisting(Name("Quit"), out EventWaitHandle? requested))
        {
            return true;
        }

        using (requested)
        {
            if (!EventWaitHandle.TryOpenExisting(Name("Exited"), out EventWaitHandle? completed))
            {
                return false;
            }

            using (completed)
            {
                requested.Set();
                return completed.WaitOne(TimeSpan.FromSeconds(10));
            }
        }
    }

    private static string Name(string suffix)
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        using Process process = Process.GetCurrentProcess();
        string identifier = $"{identity.User?.Value}|{process.SessionId}|{Environment.ProcessPath?.ToUpperInvariant()}";
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identifier)));
        return $@"Local\DrinkWater.{hash}.{suffix}";
    }
}
