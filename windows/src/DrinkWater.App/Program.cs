using DrinkWater.App.Diagnostics;
using DrinkWater.App.Persistence;
using DrinkWater.App.Tray;
using DrinkWater.App.WindowsIntegration;

namespace DrinkWater.App;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DrinkWater");
        AppLog log = new(dataDirectory);
        try
        {
            if (args.Length == 1 && args[0] == "--quit")
            {
                return ShutdownSignal.RequestExit() ? 0 : 1;
            }

            if (args.Length != 0)
            {
                log.Write("Unsupported launch arguments; application was not started.");
                return 2;
            }

            using SingleInstance instance = new();
            if (!instance.IsFirstInstance)
            {
                return 0;
            }

            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, eventArgs) =>
            {
                log.Write("Unexpected UI failure; application is exiting.", eventArgs.Exception);
                Environment.ExitCode = 1;
                Application.Exit();
            };
            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
                log.Write("Fatal application failure.", eventArgs.ExceptionObject as Exception);

            SettingsStore settings = new(Path.Combine(dataDirectory, "settings.json"));
            SettingsLoadResult loaded = settings.Load();
            if (loaded.Warning is not null)
            {
                log.Write(loaded.Warning);
            }

            SloganStore slogans = new(Path.Combine(dataDirectory, "slogans.txt"));
            NetworkSettingsStore networkSettings = new(Path.Combine(dataDirectory, "network.json"));
            using TrayApplicationContext context = new(settings, loaded, log, slogans, networkSettings);
            Application.Run(context);
            return Environment.ExitCode;
        }
        catch (Exception error)
        {
            log.Write("Drink Water could not start or shut down cleanly.", error);
            return 1;
        }
    }
}
