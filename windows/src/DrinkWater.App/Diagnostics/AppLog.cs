using System.Diagnostics;
using System.Text;

namespace DrinkWater.App.Diagnostics;

public sealed class AppLog(string directory)
{
    private const long MaximumBytes = 512 * 1024;
    private readonly object _gate = new();

    public void Write(string message, Exception? exception = null)
    {
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "drink-water.log");
                if (File.Exists(path) && new FileInfo(path).Length >= MaximumBytes)
                {
                    File.Move(path, path + ".1", overwrite: true);
                }

                string entry = $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}";
                if (exception is not null)
                {
                    entry += exception + Environment.NewLine;
                }

                File.AppendAllText(path, entry, Encoding.UTF8);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Trace.TraceError("Drink Water could not write its diagnostic log: {0}", error);
            }
        }
    }
}
