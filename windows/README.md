# Windows app

Tray application for Windows 10 version 1809 or later and Windows 11 on x64.

## Build and verify

Requires .NET 10 SDK and Inno Setup. From the repository root:

```powershell
pwsh -NoProfile -File windows/scripts/Get-BuildTools.ps1
pwsh -NoProfile -File windows/scripts/Verify.ps1
pwsh -NoProfile -File windows/scripts/Build.ps1
```

Installers are written to `windows/artifacts/installer/`. See the [main README](../README.md) for usage and downloads.
