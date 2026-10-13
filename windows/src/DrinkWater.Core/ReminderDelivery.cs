using System.ComponentModel;

namespace DrinkWater.Core;

public sealed class ReminderDelivery(Action notify, Action playSound, Action<string, Exception> reportError)
{
    public void Deliver(bool soundEnabled)
    {
        Attempt(notify, "The reminder notification could not be submitted.");
        if (soundEnabled)
        {
            Attempt(playSound, "The water sound could not be played.");
        }
    }

    private void Attempt(Action action, string message)
    {
        try
        {
            action();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
        {
            reportError(message, error);
        }
    }
}
