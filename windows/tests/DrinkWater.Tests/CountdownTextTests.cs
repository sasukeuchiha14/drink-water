using DrinkWater.Core;

namespace DrinkWater.Tests;

public sealed class CountdownTextTests
{
    [Theory]
    [InlineData(0, "Next reminder in 00:00")]
    [InlineData(1, "Next reminder in 00:01")]
    [InlineData(1800, "Next reminder in 30:00")]
    [InlineData(3600, "Next reminder in 1:00:00")]
    [InlineData(-1, "Next reminder in 00:00")]
    [InlineData(0.1, "Next reminder in 00:01")]
    public void CountdownDisplaysHoursAndRoundsUp(double seconds, string expected) =>
        Assert.Equal(expected, CountdownText.Format(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void DisabledStatusHasNoCountdown() => Assert.Equal("Reminders disabled", CountdownText.Format(null));
}
