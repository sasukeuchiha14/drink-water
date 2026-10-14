namespace DrinkWater.Core;

public static class ReminderSlogans
{
    public static IReadOnlyList<string> Defaults { get; } = Array.AsReadOnly<string>(
    [
        "Time for a drink of water.",
        "Small sips, steady habits.",
        "Pause. Sip. Carry on.",
        "Your next sip starts here.",
        "Take a sip and refresh.",
        "Here's to your next sip."
    ]);
}

public sealed class ReminderSloganSelector(Random? random = null)
{
    private readonly Random _random = random ?? Random.Shared;
    private string? _lastMessage;

    public string Next(IReadOnlyList<string> messages, bool randomize)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Count == 0)
        {
            throw new ArgumentException("At least one slogan is required.", nameof(messages));
        }

        string[] candidates = randomize && messages.Count > 1
            ? messages.Where(message => message != _lastMessage).ToArray()
            : [];
        string selected = candidates.Length > 0 ? candidates[_random.Next(candidates.Length)] : messages[0];
        _lastMessage = selected;
        return selected;
    }
}
