using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Security;
using DrinkWater.App.Diagnostics;
using DrinkWater.App.Persistence;
using DrinkWater.App.Updates;
using DrinkWater.App.WindowsIntegration;
using DrinkWater.Core;

namespace DrinkWater.App.Tray;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AppLog _log;
    private readonly SettingsStore _settings;
    private readonly bool _canSave;
    private readonly ReminderSchedule _schedule;
    private readonly StartupRegistration _startup;
    private readonly Icon _icon;
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _countdown = new() { Enabled = false };
    private readonly ToolStripMenuItem _enabled = new("Enable reminders");
    private readonly ToolStripMenuItem _soundEnabled = new("Play water sound");
    private readonly ToolStripMenuItem _startupEnabled = new("Start with Windows");
    private readonly ToolStripMenuItem _remindNow = new("Remind me now");
    private readonly ToolStripMenuItem _customIntervalLabel = new("Custom interval (minutes)") { Enabled = false };
    private readonly ToolStripTextBox _customIntervalInput = new() { AutoSize = false, Width = 180, MaxLength = 4, ToolTipText = "Enter 1 to 1440 minutes, then press Enter or choose Set custom interval." };
    private readonly ToolStripMenuItem _customIntervalApply = new("Set custom interval");
    private readonly ToolStripMenuItem _customIntervalError = new("Enter 1 to 1440 whole minutes.") { Enabled = false, Visible = false };
    private readonly ToolStripMenuItem _randomSlogans = new("Random slogans");
    private readonly ToolStripMenuItem _sloganStatus = new() { Enabled = false };
    private readonly SloganStore _slogans;
    private readonly ReminderSloganSelector _sloganSelector = new();
    private string? _sloganWarning;
    private readonly ToolStripMenuItem _checkUpdates = new("Check for updates...");
    private readonly ToolStripMenuItem _updateStatus = new() { Enabled = false };
    private readonly ToolStripMenuItem _downloadUpdate = new() { Visible = false };
    private readonly HttpClient _updateClient = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };
    private readonly CancellationTokenSource _lifetime = new();
    private readonly ReleaseVersion _currentVersion;
    private UpdateCheckResult? _availableUpdate;
    private bool _checkingUpdates;
    private readonly Dictionary<int, ToolStripMenuItem> _intervals = [];
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly SessionWindow _session = new();
    private readonly ShutdownSignal _shutdown = new();
    private readonly WaterSound _sound;
    private readonly ReminderDelivery _delivery;
    private string? _pendingNotice;
    private bool _disposed;

    public TrayApplicationContext(SettingsStore settings, SettingsLoadResult loaded, AppLog log, SloganStore slogans)
    {
        _log = log;
        _settings = settings;
        _slogans = slogans;
        _canSave = loaded.CanSave;
        _pendingNotice = loaded.Warning;
        _schedule = new(loaded.Preferences);
        Version installed = Assembly.GetExecutingAssembly().GetName().Version
            ?? throw new InvalidOperationException("The installed version is unavailable.");
        _currentVersion = new(installed.Major, installed.Minor, installed.Build);
        _updateStatus.Text = $"Version {_currentVersion}";
        _startup = new(Environment.ProcessPath ?? throw new InvalidOperationException("The application path is unavailable."));
        _sound = new(Path.Combine(AppContext.BaseDirectory, "Assets", "water.mp3"));
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DrinkWater.App.logo.ico")
            ?? throw new InvalidOperationException("The application icon is missing.");
        using Icon original = new(stream);
        _icon = (Icon)original.Clone();
        _tray = new() { Icon = _icon, ContextMenuStrip = _menu, Visible = true, Text = "Drink Water" };
        _delivery = new(() => Notify("Drink Water", NextSlogan()), _sound.Play, _log.Write);
        BuildMenu();
        _menu.Opening += (_, _) => RefreshMenu(includeStartup: true);
        _timer.Tick += Tick;
        _session.Suspending += (_, _) =>
        {
            _schedule.Suspend();
            StopSound();
        };
        _session.Resuming += (_, _) =>
        {
            _schedule.Resume();
            RefreshMenu(includeStartup: false);
        };
        _session.Closing += (_, _) => ExitThread();
        _sound.PlaybackFailed += (_, error) => _log.Write("Stopping water audio failed.", error);
        RefreshMenu(includeStartup: true);
        _timer.Start();
        _log.Write("Tray application started.");
    }

    private void BuildMenu()
    {
        ToolStripMenuItem intervalMenu = new("Interval");
        foreach (int minutes in new[] { 10, 30, 60 })
        {
            ToolStripMenuItem item = new(minutes == 60 ? "1 hour" : $"{minutes} minutes");
            item.Click += (_, _) => ChangePreferences(_schedule.Preferences with { IntervalMinutes = minutes });
            _intervals.Add(minutes, item);
            intervalMenu.DropDownItems.Add(item);
        }

        intervalMenu.DropDownItems.AddRange([new ToolStripSeparator(), _customIntervalLabel,
            _customIntervalInput, _customIntervalApply, _customIntervalError]);
        intervalMenu.DropDownOpening += (_, _) =>
        {
            _customIntervalInput.Text = _schedule.Preferences.IntervalMinutes.ToString(CultureInfo.InvariantCulture);
            _customIntervalError.Visible = false;
        };
        _customIntervalApply.Click += (_, _) => ApplyCustomInterval(intervalMenu);
        _customIntervalInput.TextChanged += (_, _) =>
        {
            _customIntervalApply.Enabled = ReminderIntervals.TryParse(_customIntervalInput.Text, out _);
            _customIntervalError.Visible = false;
        };
        _customIntervalInput.KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.Enter)
            {
                args.SuppressKeyPress = true;
                ApplyCustomInterval(intervalMenu);
            }
        };

        ToolStripMenuItem sloganMenu = new("Notification slogans");
        _randomSlogans.Click += (_, _) => ChangePreferences(_schedule.Preferences with { RandomSlogans = !_schedule.Preferences.RandomSlogans });
        ToolStripMenuItem editSlogans = new("Edit slogan list...") { ToolTipText = "One slogan per line. Save to apply changes at the next reminder." };
        editSlogans.Click += (_, _) => OpenSlogans(openFolder: false);
        ToolStripMenuItem sloganFolder = new("Open slogans folder...");
        sloganFolder.Click += (_, _) => OpenSlogans(openFolder: true);
        sloganMenu.DropDownItems.AddRange([_randomSlogans, editSlogans, sloganFolder, new ToolStripSeparator(), _sloganStatus]);
        sloganMenu.DropDownOpening += (_, _) => RefreshSlogans();

        _enabled.Click += (_, _) => ChangePreferences(_schedule.Preferences with { RemindersEnabled = !_schedule.Preferences.RemindersEnabled });
        _soundEnabled.Click += (_, _) => ChangePreferences(_schedule.Preferences with { SoundEnabled = !_schedule.Preferences.SoundEnabled });
        _startupEnabled.Click += ToggleStartup;
        _remindNow.Click += (_, _) =>
        {
            if (_schedule.RequestReminder())
            {
                _delivery.Deliver(_schedule.Preferences.SoundEnabled);
                RefreshMenu(includeStartup: false);
            }
        };
        ToolStripMenuItem startupSettings = new("Windows startup settings...");
        startupSettings.Click += (_, _) => OpenStartupSettings();
        ToolStripMenuItem quit = new("Quit");
        quit.Click += (_, _) => ExitThread();
        _checkUpdates.Click += async (_, _) => await CheckForUpdatesAsync();
        _downloadUpdate.Click += (_, _) => OpenUpdateDownload();
        _menu.Items.AddRange([_countdown, new ToolStripSeparator(), intervalMenu, sloganMenu, new ToolStripSeparator(),
            _enabled, _soundEnabled, _startupEnabled, startupSettings, new ToolStripSeparator(), _remindNow,
            _checkUpdates, _updateStatus, _downloadUpdate, new ToolStripSeparator(), quit]);
    }

    private void ApplyCustomInterval(ToolStripMenuItem intervalMenu)
    {
        if (!ReminderIntervals.TryParse(_customIntervalInput.Text, out int minutes))
        {
            _customIntervalError.Visible = true;
            _customIntervalInput.TextBox.Focus();
            return;
        }

        ChangePreferences(_schedule.Preferences with { IntervalMinutes = minutes });
        intervalMenu.DropDown.Close(ToolStripDropDownCloseReason.ItemClicked);
    }

    private void Tick(object? sender, EventArgs args)
    {
        if (_shutdown.IsRequested)
        {
            ExitThread();
            return;
        }

        if (_pendingNotice is not null)
        {
            Notify("Drink Water settings", _pendingNotice);
            _pendingNotice = null;
        }

        if (_schedule.Tick())
        {
            _delivery.Deliver(_schedule.Preferences.SoundEnabled);
        }

        RefreshMenu(includeStartup: false);
    }

    private void ChangePreferences(ReminderPreferences preferences)
    {
        _schedule.UpdatePreferences(preferences);
        if (!preferences.RemindersEnabled || !preferences.SoundEnabled)
        {
            StopSound();
        }

        try
        {
            if (_canSave)
            {
                _settings.Save(preferences);
            }
            else
            {
                Notify("Drink Water settings", "These settings cannot be saved. Your change applies to this session only.");
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            _log.Write("Saving preferences failed.", error);
            Notify("Drink Water settings", "Your change applies to this session, but could not be saved. Check the app's settings folder permissions.");
        }

        RefreshMenu(includeStartup: false);
    }

    private void RefreshMenu(bool includeStartup)
    {
        string countdown = CountdownText.Format(_schedule.Remaining);
        _countdown.Text = countdown;
        _tray.Text = "Drink Water — " + countdown;
        _enabled.Checked = _schedule.Preferences.RemindersEnabled;
        _soundEnabled.Checked = _schedule.Preferences.SoundEnabled;
        _remindNow.Enabled = _schedule.Preferences.RemindersEnabled;
        foreach ((int minutes, ToolStripMenuItem item) in _intervals)
        {
            item.Checked = minutes == _schedule.Preferences.IntervalMinutes;
        }

        int interval = _schedule.Preferences.IntervalMinutes;
        _customIntervalLabel.Text = _intervals.ContainsKey(interval) ? "Custom interval (minutes)" : $"Custom interval: {interval} minutes";
        _randomSlogans.Checked = _schedule.Preferences.RandomSlogans;

        if (includeStartup)
        {
            try
            {
                StartupState state = _startup.Read();
                _startupEnabled.Checked = state == StartupState.Enabled;
                _startupEnabled.Text = state switch
                {
                    StartupState.DisabledByWindows => "Start with Windows (disabled in Windows)",
                    StartupState.Unknown => "Start with Windows (check Windows settings)",
                    StartupState.DifferentApplication => "Start with Windows (another installation)",
                    _ => "Start with Windows"
                };
            }
            catch (Exception error) when (IsStartupError(error))
            {
                _startupEnabled.Checked = false;
                _startupEnabled.Text = "Start with Windows (unavailable)";
                _log.Write("Reading startup registration failed.", error);
            }
        }
    }

    private void ToggleStartup(object? sender, EventArgs args)
    {
        try
        {
            StartupState state = _startup.Read();
            if (state is StartupState.DisabledByWindows or StartupState.Unknown)
            {
                OpenStartupSettings();
            }
            else
            {
                _startup.SetEnabled(state != StartupState.Enabled);
            }
        }
        catch (Exception error) when (IsStartupError(error))
        {
            _log.Write("Changing startup registration failed.", error);
            Notify("Drink Water startup", error is InvalidOperationException ? error.Message : "Windows could not change startup registration. Check Windows startup settings.");
        }

        RefreshMenu(includeStartup: true);
    }

    private void OpenStartupSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true });
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException)
        {
            _log.Write("Opening Windows startup settings failed.", error);
            Notify("Drink Water startup", "Open Windows Settings > Apps > Startup to manage Drink Water.");
        }
    }

    private static bool IsStartupError(Exception error) =>
        error is IOException or UnauthorizedAccessException or SecurityException or InvalidOperationException or ArgumentException;

    private SloganLoadResult RefreshSlogans()
    {
        SloganLoadResult loaded = _slogans.Load();
        if (loaded.Warning != _sloganWarning && loaded.Warning is not null)
        {
            _log.Write(loaded.Warning);
        }

        _sloganWarning = loaded.Warning;
        _sloganStatus.Text = loaded.Warning is not null ? "List unavailable — using built-in slogans" : $"{loaded.Messages.Count} slogans — save edits to apply";
        return loaded;
    }

    private string NextSlogan() => _sloganSelector.Next(RefreshSlogans().Messages, _schedule.Preferences.RandomSlogans);

    private void OpenSlogans(bool openFolder)
    {
        try
        {
            _slogans.EnsureExists();
            string target = openFolder ? Path.GetDirectoryName(_slogans.FilePath)! : _slogans.FilePath;
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
        {
            _log.Write("Opening the slogan file failed.", error);
            Notify("Drink Water slogans", "Open %LOCALAPPDATA%\\DrinkWater\\slogans.txt in a text editor. Use one slogan per line and save your changes.");
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        if (_checkingUpdates || _disposed)
        {
            return;
        }

        _checkingUpdates = true;
        _checkUpdates.Enabled = false;
        _checkUpdates.Text = "Checking for updates...";
        _updateStatus.Text = $"Version {_currentVersion} — checking...";
        try
        {
            UpdateCheckResult result = await new UpdateChecker(_updateClient).CheckAsync(_currentVersion, _lifetime.Token);
            if (_disposed)
            {
                return;
            }

            _availableUpdate = result.UpdateAvailable ? result : null;
            _downloadUpdate.Visible = result.UpdateAvailable;
            _downloadUpdate.Text = $"Download version {result.LatestVersion}...";
            _updateStatus.Text = result.UpdateAvailable ? $"Version {result.LatestVersion} available" : $"Version {_currentVersion} — up to date";
            Notify("Drink Water updates", result.UpdateAvailable
                ? $"Version {result.LatestVersion} is available. Right-click the tray icon and choose Download version {result.LatestVersion}. Run the installer to update and keep your settings."
                : $"You're up to date. Installed version: {_currentVersion}.");
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            // Normal shutdown cancels the request without showing a notification.
        }
        catch (HttpRequestException error)
        {
            _log.Write("Checking for updates failed.", error);
            if (!_disposed)
            {
                _updateStatus.Text = $"Version {_currentVersion} — update check failed";
                Notify("Drink Water updates", "Couldn't check for updates. Check your connection and try again. Any previously found download remains available.");
            }
        }
        finally
        {
            _checkingUpdates = false;
            if (!_disposed)
            {
                _checkUpdates.Text = "Check for updates...";
                _checkUpdates.Enabled = true;
            }
        }
    }

    private void OpenUpdateDownload()
    {
        if (_availableUpdate is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_availableUpdate.DownloadUrl.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException)
        {
            _log.Write("Opening the update download failed.", error);
            Notify("Drink Water updates", "Open the Drink Water website to download the latest installer.");
        }
    }

    private void Notify(string title, string message) => _tray.ShowBalloonTip(5000, title, message, ToolTipIcon.None);

    private void StopSound()
    {
        try
        {
            _sound.Stop();
        }
        catch (Win32Exception error)
        {
            _log.Write("Stopping water audio failed.", error);
        }
    }

    protected override void ExitThreadCore()
    {
        DisposeResources();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeResources();
        }

        base.Dispose(disposing);
    }

    private void DisposeResources()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetime.Cancel();
        _updateClient.Dispose();
        _lifetime.Dispose();
        _timer.Stop();
        _timer.Dispose();
        StopSound();
        try
        {
            _sound.Dispose();
        }
        catch (Win32Exception error)
        {
            _log.Write("Disposing audio failed; continuing shutdown.", error);
        }
        _tray.Visible = false;
        _tray.Dispose();
        _menu.Dispose();
        _icon.Dispose();
        _session.Dispose();
        _shutdown.Dispose();
        _log.Write("Tray application stopped.");
    }
}
