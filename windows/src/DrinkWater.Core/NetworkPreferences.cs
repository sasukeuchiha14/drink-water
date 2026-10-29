namespace DrinkWater.Core;

public sealed record NetworkPreferences
{
    public bool CheckForUpdatesAtStartup { get; init; } = true;
    public ProxySettings Proxy { get; init; } = new();

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Proxy);
        Proxy.Validate();
    }
}
