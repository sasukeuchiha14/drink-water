using DrinkWater.Core;

namespace DrinkWater.Tests;

public sealed class ReminderIntervalTests
{
    [Theory]
    [InlineData("1", 1)]
    [InlineData("75", 75)]
    [InlineData("1440", 1440)]
    [InlineData(" 30 ", 30)]
    public void AcceptsWholeMinutesWithinBounds(string input, int expected)
    {
        Assert.True(ReminderIntervals.TryParse(input, out int minutes));
        Assert.Equal(expected, minutes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1441")]
    [InlineData("1.5")]
    [InlineData("1e2")]
    [InlineData("2147483648")]
    public void RejectsInvalidInput(string? input) => Assert.False(ReminderIntervals.TryParse(input, out _));
}
