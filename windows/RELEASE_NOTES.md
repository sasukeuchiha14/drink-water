# Drink Water 1.0.0

A Windows tray application with:

- A live countdown to the next reminder.
- Quick intervals of 10 minutes, 30 minutes, or one hour.
- Reminder and water-sound controls.
- Optional launch at sign-in, managed from the tray or Windows settings.
- Saved preferences, single-instance handling, and safe sleep/resume behavior.
- A per-user installer with a bundled .NET runtime.

Download `DrinkWater-Setup.exe` for Windows 10 version 1809 or later or Windows 11,
on x64. The installer is unsigned. The desktop application has no network
functionality or telemetry.

`SHA256SUMS.txt` contains the installer checksum. In PowerShell, run:

```powershell
Get-FileHash -Algorithm SHA256 .\DrinkWater-Setup.exe
```

Compare the result with the checksum before running the installer.
