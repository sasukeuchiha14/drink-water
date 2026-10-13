using DrinkWater.Core;

namespace DrinkWater.Tests;

public sealed class ReminderDeliveryTests
{
    [Fact]
    public void NotificationFailureDoesNotPreventAudio()
    {
        bool played = false;
        List<string> failures = [];
        ReminderDelivery delivery = new(() => throw new InvalidOperationException("Notification unavailable"),
            () => played = true, (message, _) => failures.Add(message));
        delivery.Deliver(soundEnabled: true);
        Assert.True(played);
        Assert.Single(failures);
    }

    [Fact]
    public void SoundFailureIsReportedAfterSubmittingTheNotification()
    {
        bool notified = false;
        List<Exception> failures = [];
        ReminderDelivery delivery = new(() => notified = true,
            () => throw new IOException("Audio unavailable"), (_, error) => failures.Add(error));
        delivery.Deliver(soundEnabled: true);
        Assert.True(notified);
        Assert.IsType<IOException>(Assert.Single(failures));
    }

    [Fact]
    public void DisabledSoundIsNeverPlayed()
    {
        bool notified = false;
        ReminderDelivery delivery = new(() => notified = true,
            () => throw new InvalidOperationException("Sound should not play"), (_, _) => Assert.Fail("Unexpected failure"));
        delivery.Deliver(soundEnabled: false);
        Assert.True(notified);
    }

    [Fact]
    public void ProgrammingErrorsAreNotHidden()
    {
        ReminderDelivery delivery = new(() => throw new NullReferenceException(), () => { }, (_, _) => { });
        Assert.Throws<NullReferenceException>(() => delivery.Deliver(soundEnabled: true));
    }
}
