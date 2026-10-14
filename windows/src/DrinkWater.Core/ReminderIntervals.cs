using System.Globalization;

namespace DrinkWater.Core;

public static class ReminderIntervals
{
    public const int MinimumMinutes = 1;
    public const int MaximumMinutes = 24 * 60;

    public static bool TryParse(string? text, out int minutes) =>
        int.TryParse(text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out minutes)
        && minutes is >= MinimumMinutes and <= MaximumMinutes;
}
