# Drink Water

A free Windows tray app that reminds you to drink water.

**[Download for Windows](https://github.com/sasukeuchiha14/drink-water/releases/latest/download/DrinkWater-Setup.exe)** · [Website](https://drinkwater.hardikgarg.me/) · [Release notes](https://github.com/sasukeuchiha14/drink-water/releases/latest)

Supports Windows 10 version 1809 or later and Windows 11 on x64. The installer includes .NET and installs for your user account. Current releases are unsigned.

## Features

- Live countdown and quick intervals of 10 minutes, 30 minutes, or one hour.
- Custom intervals from 1 minute to 24 hours.
- Random notification slogans or a fixed message from your editable local list.
- Reminder, water sound, and Windows startup controls.
- Optional startup update checks and manual checks; updates stay optional.
- App-specific HTTP, HTTPS, and SOCKS5 proxies with protected saved passwords.

## Use

Right-click the glass beside your clock. **Interval** includes a custom minutes field. Under **Notification slogans**, use **Random slogans** to shuffle messages and **Edit slogan list** to open `slogans.txt`. Write one message per line and save; edits apply at the next reminder. With random mode off, the first slogan is used.

**Proxy**, below the update controls, includes system, direct, and custom connections. Enter a server, port, and optional credentials, then apply. Proxy settings cover the app's update checks; installer downloads use your browser's network settings.

Preferences, slogans, and logs are stored in `%LOCALAPPDATA%\DrinkWater`. The app has no accounts or telemetry. It checks for updates at launch unless you turn off **Check for updates at startup**.

To update, choose **Check for updates**, download the newer installer, and run it over your existing installation. Preferences and startup settings are retained; uninstalling first is unnecessary.

## Build

Windows requires .NET 10 SDK and Inno Setup. Node.js 22 or later is needed for the website.

```powershell
pwsh -NoProfile -File windows/scripts/Get-BuildTools.ps1
pwsh -NoProfile -File windows/scripts/Verify.ps1
pwsh -NoProfile -File windows/scripts/Build.ps1
cd frontend
npm run check
npm run build
```

- `windows/`: C# app, tests, installer, and build scripts.
- `frontend/`: website, public assets, and update API.
- `drink_water.py`: original Python implementation.

[MIT license](LICENSE) · [Hardik Garg](https://hardikgarg.me/)
