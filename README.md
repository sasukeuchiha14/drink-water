# Drink Water

A free Windows tray app that reminds you to take your next sip. Choose a
10-minute, 30-minute, or one-hour interval, pause reminders, and manage launch at
sign-in from the icon beside your clock.

**[Download the Windows installer](https://github.com/sasukeuchiha14/drink-water/releases/latest/download/DrinkWater-Setup.exe)**
· [Release notes and checksum](https://github.com/sasukeuchiha14/drink-water/releases/latest)

The first release targets Windows 10 version 1809 or later and Windows 11 on x64.
The installer includes .NET; Python and a separate .NET installation are not
required. The current installer is unsigned.

## Features

- A live next-reminder countdown in the tray menu.
- Quick intervals of 10 minutes, 30 minutes, and one hour.
- Reminder and bundled water-sound controls.
- Optional launch at sign-in, managed in the app or Windows settings.
- Saved preferences, one instance per user session, and sleep/resume handling.
- A per-user installer, shortcuts, upgrades, and clean uninstallation.
- Manual update checks with a trusted GitHub installer download.

The desktop app connects to the website only when you choose **Check for updates**,
sending its installed version. There are no accounts or telemetry. Settings
and diagnostic logs stay on your PC. Windows controls notification visibility,
Do Not Disturb, and its own notification sound.

To update, run the newer installer over the existing installation. It preserves
your preferences and startup choice; uninstalling first is unnecessary. Version
1.0.0 users download the new installer from the website once to get update checks.

## Repository

```text
windows/       C# app, tests, installer, and build scripts
frontend/      Download website and Netlify deployment instructions
.github/       Application checks and automatic release publishing
logo.ico       Application, fixed tray, and website icon
water.mp3      Original reminder sound
drink_water.py Original Python implementation
netlify.toml   Website build, routing, and security headers
LICENSE        MIT license
```

The icon and audio are versioned source assets. Built installers are uploaded as
GitHub release assets rather than committed to the repository. Local SDKs,
package caches, build files, browser output, and signing credentials are ignored.

## Windows development

Read the [Windows instructions](windows/README.md). From the repository root:

```powershell
pwsh -NoProfile -File windows/scripts/Get-BuildTools.ps1
pwsh -NoProfile -File windows/scripts/Verify.ps1
pwsh -NoProfile -File windows/scripts/Build.ps1
```

The bootstrap verifies the .NET SDK checksum and the installer compiler's
publisher signature. The project uses nullable references, compiler warnings as
errors, SDK analyzers, formatting checks, and focused tests.

## Download website and Netlify

Read the [frontend instructions](frontend/README.md). The site uses static
HTML/CSS/JavaScript, light and dark themes, an interactive browser preview, a
Netlify update endpoint, and GitHub release downloads.

```powershell
cd frontend
npm run check
npm run build
npm run preview
```

Import this GitHub repository into Netlify. The root `netlify.toml` configures
base directory `frontend`, publish directory `dist`, and the build command.
Add `drinkwater.hardikgarg.me` as the primary domain and
`drink-water.hardikgarg.me` as an alias, then configure your DNS and HTTPS in
Netlify. Website pages on the alias redirect to the primary domain; the update
API answers on both hosts. See the frontend README for the endpoint contract.

## Publishing a Windows release

Update `windows/Directory.Build.props` and `windows/RELEASE_NOTES.md`, verify the
changes, commit them, then push a matching version tag:

```powershell
git tag v1.1.0
git push origin v1.1.0
```

The release workflow builds and checks the Windows app, compiles the installer,
and publishes `DrinkWater-Setup.exe` and `SHA256SUMS.txt`. The website's stable
download link automatically selects the latest published release. GitHub Actions
must be enabled for the repository. Public release signing requires your own
certificate; signing credentials stay outside Git.

## Original Python version

The original script retains its fixed 30-minute reminder behavior:

```powershell
pip install plyer pygame
python drink_water.py
```

A future Linux application can live separately under `linux/` using another
language.

Licensed under [MIT](LICENSE). Author: [Hardik Garg](https://hardikgarg.me).
