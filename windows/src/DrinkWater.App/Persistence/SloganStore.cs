using System.Text;
using DrinkWater.Core;

namespace DrinkWater.App.Persistence;

public sealed record SloganLoadResult(IReadOnlyList<string> Messages, string? Warning);

public sealed class SloganStore(string path)
{
    public const int MaximumBytes = 64 * 1024;
    public const int MaximumSlogans = 100;
    public const int MaximumMessageLength = 200;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public string FilePath { get; } = Path.GetFullPath(path);

    public void EnsureExists()
    {
        if (File.Exists(FilePath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        FileStream stream;
        try
        {
            stream = new(FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        }
        catch (IOException) when (File.Exists(FilePath))
        {
            return;
        }

        using (stream)
        using (StreamWriter writer = new(stream, Utf8))
        {
            writer.WriteLine("# One slogan per line. Save this file to apply changes at the next reminder.");
            foreach (string message in ReminderSlogans.Defaults)
            {
                writer.WriteLine(message);
            }
        }
    }

    public SloganLoadResult Load()
    {
        try
        {
            EnsureExists();
            using FileStream stream = new(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length > MaximumBytes)
            {
                throw new InvalidDataException("The slogan file exceeds 64 KB.");
            }

            byte[] bytes = new byte[MaximumBytes + 1];
            int length = 0;
            int count;
            while (length < bytes.Length && (count = stream.Read(bytes, length, bytes.Length - length)) > 0)
            {
                length += count;
            }

            if (length > MaximumBytes)
            {
                throw new InvalidDataException("The slogan file exceeds 64 KB.");
            }

            string text = Utf8.GetString(bytes, 0, length).TrimStart('\uFEFF');
            List<string> messages = [];
            HashSet<string> unique = new(StringComparer.Ordinal);
            foreach (string line in text.Split('\n'))
            {
                string message = line.Trim();
                if (message.Length == 0 || message.StartsWith('#'))
                {
                    continue;
                }

                if (message.Length > MaximumMessageLength || message.Any(char.IsControl))
                {
                    throw new InvalidDataException("Use one slogan per line, with at most 200 characters.");
                }

                if (unique.Add(message))
                {
                    messages.Add(message);
                }

                if (messages.Count > MaximumSlogans)
                {
                    throw new InvalidDataException("The slogan file contains more than 100 slogans.");
                }
            }

            if (messages.Count == 0)
            {
                throw new InvalidDataException("Add at least one slogan to the file.");
            }

            return new(messages.AsReadOnly(), null);
        }
        catch (InvalidDataException error)
        {
            return new(ReminderSlogans.Defaults, error.Message + " Using built-in slogans.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or DecoderFallbackException)
        {
            return new(ReminderSlogans.Defaults, "The slogan file cannot be read. Using built-in slogans.");
        }
    }
}
