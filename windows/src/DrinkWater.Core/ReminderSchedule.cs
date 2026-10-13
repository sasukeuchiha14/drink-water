namespace DrinkWater.Core;

public sealed class ReminderSchedule
{
    private readonly TimeProvider _clock;
    private long _intervalStarted;
    private bool _suspended;

    public ReminderSchedule(ReminderPreferences preferences, TimeProvider? clock = null)
    {
        preferences.Validate();
        Preferences = preferences;
        _clock = clock ?? TimeProvider.System;
        Restart();
    }

    public ReminderPreferences Preferences { get; private set; }

    public TimeSpan? Remaining
    {
        get
        {
            if (!Preferences.RemindersEnabled || _suspended)
            {
                return null;
            }

            TimeSpan remaining = TimeSpan.FromMinutes(Preferences.IntervalMinutes)
                - _clock.GetElapsedTime(_intervalStarted);
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public void UpdatePreferences(ReminderPreferences preferences)
    {
        preferences.Validate();
        bool restart = preferences.IntervalMinutes != Preferences.IntervalMinutes
            || (preferences.RemindersEnabled && !Preferences.RemindersEnabled);
        Preferences = preferences;
        if (restart)
        {
            Restart();
        }
    }

    public bool Tick()
    {
        if (Remaining != TimeSpan.Zero)
        {
            return false;
        }

        Restart();
        return true;
    }

    public bool RequestReminder()
    {
        if (!Preferences.RemindersEnabled || _suspended)
        {
            return false;
        }

        Restart();
        return true;
    }

    public void Suspend() => _suspended = true;

    public void Resume()
    {
        _suspended = false;
        Restart();
    }

    private void Restart() => _intervalStarted = _clock.GetTimestamp();
}
