using DrinkWater.App.WindowsIntegration;

namespace DrinkWater.Tests;

public sealed class StartupPolicyTests
{
    private const string Expected = "\"C:\\Users\\Example User\\Apps\\DrinkWater.exe\"";

    [Fact]
    public void StartupPathsWithSpacesAreQuoted() =>
        Assert.Equal(Expected, StartupPolicy.CommandFor(@"C:\Users\Example User\Apps\DrinkWater.exe"));

    [Theory]
    [InlineData("DrinkWater.exe")]
    [InlineData(@"C:\Apps\dotnet.exe")]
    [InlineData("C:\\Apps\\\"DrinkWater.exe")]
    public void InvalidStartupPathsAreRejected(string path) =>
        Assert.Throws<ArgumentException>(() => StartupPolicy.CommandFor(path));

    [Fact]
    public void OverlongStartupCommandsAreRejected() =>
        Assert.Throws<ArgumentException>(() => StartupPolicy.CommandFor(@"C:\" + new string('a', 260) + @"\DrinkWater.exe"));

    [Theory]
    [InlineData("DrinkWater.exe")]
    [InlineData(@"C:\Apps\Renamed.exe")]
    public void UnusableStartupPathsDoNotPreventCreatingTheApplicationIntegration(string path)
    {
        StartupRegistration registration = new(path);
        Assert.Throws<ArgumentException>(() => registration.Read());
        Assert.Throws<ArgumentException>(() => registration.SetEnabled(true));
    }

    [Fact]
    public void MissingRegistrationIsDisabledRegardlessOfApproval() =>
        Assert.Equal(StartupState.NotRegistered, StartupPolicy.Evaluate(null, Expected, Approval(2)));

    [Fact]
    public void AnotherInstallationIsNotTreatedAsOwned() =>
        Assert.Equal(StartupState.DifferentApplication, StartupPolicy.Evaluate("\"D:\\DrinkWater.exe\"", Expected, null));

    [Fact]
    public void RegistrationWithoutAnOverrideIsEnabled() =>
        Assert.Equal(StartupState.Enabled, StartupPolicy.Evaluate(Expected, Expected, null));

    [Theory]
    [InlineData(2, StartupState.Enabled)]
    [InlineData(6, StartupState.Enabled)]
    [InlineData(3, StartupState.DisabledByWindows)]
    [InlineData(7, StartupState.DisabledByWindows)]
    [InlineData(0, StartupState.Unknown)]
    [InlineData(99, StartupState.Unknown)]
    public void WindowsApprovalStatesAreReadWithoutAssumingUnknownFormats(uint status, StartupState expected) =>
        Assert.Equal(expected, StartupPolicy.Evaluate(Expected, Expected, Approval(status)));

    [Fact]
    public void TruncatedApprovalDataIsUnknown() =>
        Assert.Equal(StartupState.Unknown, StartupPolicy.Evaluate(Expected, Expected, [2]));

    private static byte[] Approval(uint status)
    {
        byte[] value = new byte[12];
        BitConverter.GetBytes(status).CopyTo(value, 0);
        return value;
    }
}
