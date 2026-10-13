using DrinkWater.App.Persistence;

namespace DrinkWater.Tests;

public sealed class AssetTests
{
    [Fact]
    public void ApplicationContainsTheOriginalMultiSizeIcon()
    {
        using Stream stream = typeof(SettingsStore).Assembly.GetManifestResourceStream("DrinkWater.App.logo.ico")
            ?? throw new InvalidOperationException("The icon resource was not embedded.");
        using BinaryReader reader = new(stream);
        Assert.Equal(0, reader.ReadUInt16());
        Assert.Equal(1, reader.ReadUInt16());
        Assert.True(reader.ReadUInt16() >= 2);
        using Stream iconStream = typeof(SettingsStore).Assembly.GetManifestResourceStream("DrinkWater.App.logo.ico")!;
        using Icon icon = new(iconStream);
        Assert.True(icon.Width > 0);
    }

    [Fact]
    public void WaterSoundIsIncludedInApplicationOutput() =>
        Assert.True(new FileInfo(Path.Combine(AppContext.BaseDirectory, "Assets", "water.mp3")).Length > 0);
}
