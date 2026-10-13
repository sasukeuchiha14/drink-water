namespace DrinkWater.Core;

public static class CountdownText
{
    public static string Format(TimeSpan? remaining)
    {
        if (remaining is null)
        {
            return "Reminders disabled";
        }

        TimeSpan rounded = TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling(remaining.Value.TotalSeconds)));
        string time = rounded.TotalHours >= 1 ? rounded.ToString(@"h\:mm\:ss") : rounded.ToString(@"mm\:ss");
        return $"Next reminder in {time}";
    }
}
