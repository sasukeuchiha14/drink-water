using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace DrinkWater.App.WindowsIntegration;

public sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly bool _ownsMutex;

    public SingleInstance()
    {
        using WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
        string identity = windowsIdentity.User?.Value
            ?? throw new InvalidOperationException("The Windows user identity is unavailable.");
        string identityHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        using Process process = Process.GetCurrentProcess();
        string name = $@"Local\DrinkWater.{identityHash}.{process.SessionId}";
        _mutex = new Mutex(initiallyOwned: false, name);
        try
        {
            _ownsMutex = _mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            _ownsMutex = true;
        }

        IsFirstInstance = _ownsMutex;
    }

    public bool IsFirstInstance { get; }

    public void Dispose()
    {
        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
