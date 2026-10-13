namespace DrinkWater.Core;

public sealed record ReminderPreferences
{
    public int IntervalMinutes { get; init; } = 30;

    public bool RemindersEnabled { get; init; } = true;

    public bool SoundEnabled { get; init; } = true;

    public void Validate()
    {
        if (IntervalMinutes is not (10 or 30 or 60))
        {
            throw new ArgumentOutOfRangeException(nameof(IntervalMinutes), "Choose 10, 30, or 60 minutes.");
        }
    }
}
