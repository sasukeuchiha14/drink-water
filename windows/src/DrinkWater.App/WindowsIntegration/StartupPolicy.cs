namespace DrinkWater.App.WindowsIntegration;

public enum StartupState
{
    NotRegistered,
    Enabled,
    DisabledByWindows,
    Unknown,
    DifferentApplication
}

public static class StartupPolicy
{
    public static string CommandFor(string executablePath)
    {
        if (!Path.IsPathFullyQualified(executablePath)
            || executablePath.Contains('"')
            || !string.Equals(Path.GetFileName(executablePath), "DrinkWater.exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Startup requires the absolute path to DrinkWater.exe.", nameof(executablePath));
        }

        string command = $"\"{executablePath}\"";
        if (command.Length > 260)
        {
            throw new ArgumentException("The application path is too long for Windows startup registration.", nameof(executablePath));
        }

        return command;
    }

    public static StartupState Evaluate(string? command, string expectedCommand, byte[]? approval)
    {
        if (command is null)
        {
            return StartupState.NotRegistered;
        }

        if (!string.Equals(command, expectedCommand, StringComparison.OrdinalIgnoreCase))
        {
            return StartupState.DifferentApplication;
        }

        if (approval is null)
        {
            return StartupState.Enabled;
        }

        // Explorer owns this compatibility format. Never write its approval data.
        if (approval.Length < 12)
        {
            return StartupState.Unknown;
        }

        uint status = BitConverter.ToUInt32(approval, 0);
        return status switch
        {
            2 or 6 => StartupState.Enabled,
            3 or 7 => StartupState.DisabledByWindows,
            _ => StartupState.Unknown
        };
    }
}
