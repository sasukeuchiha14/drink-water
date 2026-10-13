# Drink Water for Windows

A C# application that lives in the notification area beside the clock. There is
no main window, dashboard, console, or Windows service.

## Install and use

Run `DrinkWater-1.0.0-win-x64-Setup.exe` from `artifacts/installer`. Installation
is for the current user, without administrator rights or a separate .NET
installation. It includes a Start Menu shortcut, optional desktop shortcut, and
an uninstaller.

The first release targets Windows 10 version 1809 or later and Windows 11 on x64.
It does not support Windows 7/8 or 32-bit Windows. Microsoft's formal support
follows its [.NET 10 OS support matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md).

Launch **Drink Water** from the Start Menu. Windows may place its icon in the
overflow menu; drag it beside the clock if desired. Right-click the icon to:

- See the next reminder countdown.
- Select **10 minutes**, **30 minutes**, or **1 hour**.
- Enable or disable reminders and the bundled water sound.
- Manage launch at sign-in or open Windows startup settings.
- Send a reminder immediately, or quit.

Defaults are a 30-minute interval, reminders enabled, water sound enabled, and
startup disabled. Choosing a new interval, re-enabling reminders, or sending a
manual reminder starts a complete interval. Selecting the same interval leaves
the countdown alone.

Disabling reminders stops scheduled notifications and water audio while leaving
the tray app running. Quit leaves preferences and startup unchanged. Relaunch and
wake from sleep begin a complete interval. Missed reminders are not queued, and
only one instance runs per user session.

Windows controls notification visibility, Do Not Disturb, and its standard
notification sound. **Play water sound** controls the bundled MP3 only, playing
up to five seconds. A missing Windows media component does not prevent reminder
notification submission.

The original repository `logo.ico` supplies the executable, tray, shortcut, and
installer icon. The original `water.mp3` is bundled without modification.

## Startup and uninstall

Startup uses only the current user's Windows `Run` key. The app never changes
machine-wide registration or Windows startup approval flags. If Windows Settings
or Task Manager disabled it, use **Windows startup settings...** to re-enable it.
Unknown approval formats are displayed as uncertain.

Updates preserve preferences and startup registration. Uninstall closes this
installation, removes its installed files and shortcuts, and removes startup
registration only when it still points to this executable.

Settings and rotating diagnostic logs live under `%LOCALAPPDATA%\DrinkWater`.
Uninstall preserves that folder so preferences survive reinstalling. Invalid
settings are backed up; unknown schema versions are preserved without being
overwritten, and changes apply only to the running session.

## Develop and build

Use .NET 10 SDK and Inno Setup 6.3 or newer. The optional bootstrap downloads a
verified SDK and signed Inno Setup 7.1.0 locally. PowerShell 7 is recommended.

From this directory:

```powershell
pwsh -NoProfile -File scripts/Get-BuildTools.ps1
pwsh -NoProfile -File scripts/Verify.ps1
pwsh -NoProfile -File scripts/Build.ps1
```

`Verify.ps1` builds with analyzers, checks formatting, and runs the tests.
`Build.ps1` publishes the runtime and creates the installer. Add `-SkipInstaller`
to publish only, or pass `-CompilerPath 'C:/path/to/ISCC.exe'`.

Tools, package caches, binaries, and artifacts are ignored by Git. Build scripts
disable .NET CLI telemetry and keep caches in `.tools`. The application contacts
the website only for manual update checks, sending the installed version. It
has no telemetry, accounts, background update polling, or automatic installation.

```text
src/DrinkWater.Core/     Preferences, scheduling, reminder delivery
src/DrinkWater.App/      Tray, Windows integration, persistence, logging
tests/DrinkWater.Tests/  Scheduling, settings, startup policy, assets, failures
installer/              Per-user installer
scripts/                Build tools, verification, publishing
artifacts/              Generated installer and published application
```

Tests use simulated time and isolated temporary settings. They do not change
the user's startup registration or wait for a real reminder interval.

Optional desktop verification:

```powershell
pwsh -NoProfile -File scripts/Smoke-Test.ps1 -TestInstaller
```

This briefly installs inside `artifacts`, checks launch, duplicate handling,
updates, and uninstall, then removes the test installation. It refuses to replace
an existing registered installation. Run it in an interactive Windows session.

The application retains the project's [MIT license](LICENSE.txt). Published
files include license notices for the bundled .NET runtime.

## Release checks

Before distributing a release, check clean-machine installation without .NET,
tray controls, real notifications and audio, duplicate launch, Explorer restart,
sleep/resume, sign-in launch, updates, and uninstall while running.

Local builds are unsigned. Signing a public release requires your certificate;
keep private signing material outside this repository.

Author: Hardik Garg.
