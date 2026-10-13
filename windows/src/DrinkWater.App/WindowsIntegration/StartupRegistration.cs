using Microsoft.Win32;

namespace DrinkWater.App.WindowsIntegration;

public sealed class StartupRegistration(string executablePath)
{
    public const string ValueName = "DrinkWater";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovalKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    public StartupState Read()
    {
        string command = StartupPolicy.CommandFor(executablePath);
        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(RunKey);
        using RegistryKey? approvals = Registry.CurrentUser.OpenSubKey(ApprovalKey);
        object? registration = run?.GetValue(ValueName);
        if (registration is not null and not string)
        {
            return StartupState.DifferentApplication;
        }

        return StartupPolicy.Evaluate(registration as string, command, approvals?.GetValue(ValueName) as byte[]);
    }

    public void SetEnabled(bool enabled)
    {
        string command = StartupPolicy.CommandFor(executablePath);
        using RegistryKey run = Registry.CurrentUser.CreateSubKey(RunKey, writable: true)
            ?? throw new IOException("Windows startup registration is unavailable.");
        object? existing = run.GetValue(ValueName);
        if (existing is not null && !string.Equals(existing as string, command, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Another Drink Water installation owns this startup entry. Its entry was preserved.");
        }

        if (enabled)
        {
            run.SetValue(ValueName, command, RegistryValueKind.String);
        }
        else
        {
            run.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
