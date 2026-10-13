# Drink Water 1.1.0

A Windows tray application, now with:

- **Check for updates** in the tray menu, with the installed version and a
  persistent result even when Windows hides notification balloons.
- A **Download version** option for newer stable releases. It opens the official
  GitHub installer in your browser; run it to upgrade and keep your settings.
- Update endpoints on both website domains, numeric version comparison,
  a fallback host, bounded responses, and trusted download URLs.
- A website-only light/dark toggle that remembers your preference.

Version 1.0.0 does not include update checks. Download and run this installer
once to receive the new feature; uninstalling first is unnecessary.

Download `DrinkWater-Setup.exe` for Windows 10 version 1809 or later or Windows 11,
on x64. The installer is unsigned. Update checks connect only when requested;
the app has no accounts or telemetry and does not install updates automatically.

`SHA256SUMS.txt` contains the installer checksum. In PowerShell, run:

```powershell
Get-FileHash -Algorithm SHA256 .\DrinkWater-Setup.exe
```

Compare the result with the checksum before running the installer.
