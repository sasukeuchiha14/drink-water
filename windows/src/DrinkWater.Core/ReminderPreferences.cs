namespace DrinkWater.Core;

public sealed record ReminderPreferences
{
    public int IntervalMinutes { get; init; } = 30;

    public bool RemindersEnabled { get; init; } = true;

    public bool SoundEnabled { get; init; } = true;

    public bool RandomSlogans { get; init; } = true;

    public void Validate()
    {
        if (IntervalMinutes is < ReminderIntervals.MinimumMinutes or > ReminderIntervals.MaximumMinutes)
        {
            throw new ArgumentOutOfRangeException(nameof(IntervalMinutes), "Choose a whole number of minutes from 1 to 1440.");
        }
    }
}
