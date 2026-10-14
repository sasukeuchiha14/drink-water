using DrinkWater.Core;

namespace DrinkWater.Tests;

public sealed class ReminderScheduleTests
{
    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(1)]
    [InlineData(75)]
    [InlineData(1440)]
    public void ReminderIsDueOnlyAfterTheFullInterval(int minutes)
    {
        ManualClock clock = new();
        ReminderSchedule schedule = new(new() { IntervalMinutes = minutes }, clock);
        Assert.False(schedule.Tick());
        clock.Advance(TimeSpan.FromMinutes(minutes) - TimeSpan.FromMilliseconds(1));
        Assert.False(schedule.Tick());
        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(schedule.Tick());
        Assert.False(schedule.Tick());
        Assert.Equal(TimeSpan.FromMinutes(minutes), schedule.Remaining);
    }

    [Fact]
    public void DisabledRemindersNeverBecomeDueOrAllowManualReminders()
    {
        ManualClock clock = new();
        ReminderSchedule schedule = new(new() { RemindersEnabled = false }, clock);
        clock.Advance(TimeSpan.FromDays(1));
        Assert.Null(schedule.Remaining);
        Assert.False(schedule.Tick());
        Assert.False(schedule.RequestReminder());
    }

    [Fact]
    public void ReenablingStartsAFreshInterval()
    {
        ManualClock clock = new();
        ReminderSchedule schedule = new(new(), clock);
        clock.Advance(TimeSpan.FromMinutes(29));
        schedule.UpdatePreferences(schedule.Preferences with { RemindersEnabled = false });
        clock.Advance(TimeSpan.FromHours(1));
        schedule.UpdatePreferences(schedule.Preferences with { RemindersEnabled = true });
        Assert.Equal(TimeSpan.FromMinutes(30), schedule.Remaining);
        Assert.False(schedule.Tick());
    }

    [Fact]
    public void ChangingIntervalRestartsButChangingSoundDoesNot()
    {
        ManualClock clock = new();
        ReminderSchedule schedule = new(new(), clock);
        clock.Advance(TimeSpan.FromMinutes(5));
        schedule.UpdatePreferences(schedule.Preferences with { SoundEnabled = false });
        Assert.Equal(TimeSpan.FromMinutes(25), schedule.Remaining);
        schedule.UpdatePreferences(schedule.Preferences with { IntervalMinutes = 10 });
        Assert.Equal(TimeSpan.FromMinutes(10), schedule.Remaining);
    }

    [Fact]
    public void SelectingTheSameIntervalDoesNotResetTheCountdown()
    {
        ManualClock clock = new();
        ReminderSchedule schedule = new(new(), clock);
        clock.Advance(TimeSpan.FromMinutes(5));
        schedule.UpdatePreferences(schedule.Preferences with { IntervalMinutes = 30 });
        Assert.Equal(TimeSpan.FromMinutes(25), schedule.Remaining);
    }

    [Fact]
    public void DelayedTicksProduceOneReminderWithoutCatchingUp()
    {
        ManualClock clock = new();
        ReminderSchedule schedule = new(new(), clock);
        clock.Advance(TimeSpan.FromHours(5));
        Assert.True(schedule.Tick());
        Assert.False(schedule.Tick());
        Assert.Equal(TimeSpan.FromMinutes(30), schedule.Remaining);
    }

    [Fact]
    public void ManualReminderResetsTheDeadline()
    {
        ManualClock clock = new();
        ReminderSchedule schedule = new(new(), clock);
        clock.Advance(TimeSpan.FromMinutes(29));
        Assert.True(schedule.RequestReminder());
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.False(schedule.Tick());
        Assert.Equal(TimeSpan.FromMinutes(29), schedule.Remaining);
    }

    [Fact]
    public void SleepDoesNotDeliverRemindersAndResumeRestartsTheInterval()
    {
        ManualClock clock = new();
        ReminderSchedule schedule = new(new(), clock);
        schedule.Suspend();
        clock.Advance(TimeSpan.FromHours(8));
        Assert.False(schedule.Tick());
        Assert.False(schedule.RequestReminder());
        schedule.Resume();
        Assert.False(schedule.Tick());
        Assert.Equal(TimeSpan.FromMinutes(30), schedule.Remaining);
    }

    [Fact]
    public void ChangingTheWallClockDoesNotAffectAnInterval()
    {
        ManualClock clock = new();
        ReminderSchedule schedule = new(new(), clock);
        clock.UtcNow = clock.UtcNow.AddDays(-3);
        Assert.Equal(TimeSpan.FromMinutes(30), schedule.Remaining);
        clock.Advance(TimeSpan.FromMinutes(30));
        Assert.True(schedule.Tick());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1441)]
    [InlineData(int.MaxValue)]
    public void UnsupportedIntervalsAreRejected(int minutes) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReminderSchedule(new() { IntervalMinutes = minutes }));

    [Fact]
    public void ChangingSloganModeDoesNotResetTheCountdown()
    {
        ManualClock clock = new();
        ReminderSchedule schedule = new(new(), clock);
        clock.Advance(TimeSpan.FromMinutes(5));
        schedule.UpdatePreferences(schedule.Preferences with { RandomSlogans = false });
        Assert.Equal(TimeSpan.FromMinutes(25), schedule.Remaining);
    }

    private sealed class ManualClock : TimeProvider
    {
        private long _timestamp;

        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp;

        public override DateTimeOffset GetUtcNow() => UtcNow;

        public void Advance(TimeSpan duration) => _timestamp += duration.Ticks;
    }
}
