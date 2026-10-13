[CmdletBinding()]
param(
    [switch] $TestInstaller
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$windowsRoot = Split-Path -Parent $PSScriptRoot
$artifactsRoot = Join-Path $windowsRoot 'artifacts'
$executable = Join-Path $artifactsRoot 'publish/win-x64/DrinkWater.exe'
$appVersion = ([xml](Get-Content -LiteralPath (Join-Path $windowsRoot 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version
$installer = Join-Path $artifactsRoot ('installer/DrinkWater-' + $appVersion + '-win-x64-Setup.exe')
$installationDirectory = $null
$primaryProcess = $null
$originalDotnetRoot = $env:DOTNET_ROOT
$originalMultilevelLookup = $env:DOTNET_MULTILEVEL_LOOKUP
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{96EAB36C-A9DB-4051-A204-D12BFFCD4B89}_is1'

function Invoke-HiddenProcess {
    param([string] $File, [string[]] $Arguments = @())
    $parameters = @{ FilePath = $File; WindowStyle = 'Hidden'; PassThru = $true }
    if ($Arguments.Count -gt 0) { $parameters.ArgumentList = $Arguments }
    return Start-Process @parameters
}

function Wait-Success {
    param([System.Diagnostics.Process] $Process)
    if (-not $Process.WaitForExit(60000)) { throw 'The smoke-test process did not finish within one minute.' }
    if ($Process.ExitCode -ne 0) { throw ('Smoke-test process failed: ' + $Process.ExitCode) }
}

try {
    if (-not (Test-Path -LiteralPath $executable)) { throw 'Publish the application first with Build.ps1.' }
    if (Get-Process -Name DrinkWater -ErrorAction SilentlyContinue) {
        throw 'Quit the existing Drink Water instance before running this optional smoke test.'
    }

    if ($TestInstaller) {
        if (Test-Path -LiteralPath $uninstallKey) { throw 'A registered Drink Water installation already exists; it will not be modified.' }
        $installationDirectory = Join-Path $artifactsRoot ('install-smoke-' + [Guid]::NewGuid().ToString('N'))
        $resolvedDirectory = [System.IO.Path]::GetFullPath($installationDirectory)
        $resolvedRoot = [System.IO.Path]::GetFullPath($artifactsRoot) + [System.IO.Path]::DirectorySeparatorChar
        if (-not $resolvedDirectory.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'The test installation must remain inside the artifacts directory.'
        }
        $installArguments = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/TASKS=""', ('/DIR="' + $installationDirectory + '"'))
        Wait-Success (Invoke-HiddenProcess -File $installer -Arguments $installArguments)
        $executable = Join-Path $installationDirectory 'DrinkWater.exe'
        if (-not (Test-Path -LiteralPath $uninstallKey)) { throw 'The installer did not register its uninstaller.' }
        $shortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Drink Water.lnk'
        if (-not (Test-Path -LiteralPath $shortcut)) { throw 'The Start Menu shortcut was not created.' }
        Write-Host 'Per-user installation and Start Menu shortcut passed.'
    }

    # Self-contained launch must succeed without resolving an external DOTNET_ROOT.
    $env:DOTNET_ROOT = Join-Path $artifactsRoot 'no-external-runtime'
    $env:DOTNET_MULTILEVEL_LOOKUP = '0'
    $primaryProcess = Invoke-HiddenProcess -File $executable
    Start-Sleep -Milliseconds 2500
    $primaryProcess.Refresh()
    if ($primaryProcess.HasExited) { throw ('The tray app exited during launch: ' + $primaryProcess.ExitCode) }
    if ($primaryProcess.MainWindowHandle -ne [IntPtr]::Zero) { throw 'The tray app unexpectedly displayed a main window.' }
    Write-Host ('Tray launch passed. Working set: ' + [Math]::Round($primaryProcess.WorkingSet64 / 1MB, 1) + ' MiB.')

    Wait-Success (Invoke-HiddenProcess -File $executable)
    $primaryProcess.Refresh()
    if ($primaryProcess.HasExited) { throw 'Duplicate launch stopped the first instance.' }
    Write-Host 'Single-instance duplicate launch passed.'

    if ($TestInstaller) {
        $upgradeArguments = @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/TASKS=""', ('/DIR="' + $installationDirectory + '"'))
        Wait-Success (Invoke-HiddenProcess -File $installer -Arguments $upgradeArguments)
        if (-not $primaryProcess.WaitForExit(10000)) { throw 'The update did not close the running app through Restart Manager.' }
        $primaryProcess.Dispose()
        $primaryProcess = Invoke-HiddenProcess -File $executable
        Start-Sleep -Milliseconds 1500
        $primaryProcess.Refresh()
        if ($primaryProcess.HasExited) { throw 'The upgraded application did not launch.' }
        Write-Host 'Update while running and subsequent launch passed.'
    }

    Wait-Success (Invoke-HiddenProcess -File $executable -Arguments @('--quit'))
    if (-not $primaryProcess.WaitForExit(10000)) { throw 'The original tray app did not exit after its shutdown request.' }
    if ($primaryProcess.ExitCode -ne 0) { throw ('The app did not exit cleanly: ' + $primaryProcess.ExitCode) }
    Write-Host 'Graceful tray shutdown passed.'

    if ($TestInstaller) {
        $primaryProcess.Dispose()
        $primaryProcess = Invoke-HiddenProcess -File $executable
        Start-Sleep -Milliseconds 1500
        $primaryProcess.Refresh()
        if ($primaryProcess.HasExited) { throw 'The app did not launch before the running-uninstall check.' }
        $uninstaller = Join-Path $installationDirectory 'unins000.exe'
        Wait-Success (Invoke-HiddenProcess -File $uninstaller -Arguments @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART'))
        if (-not $primaryProcess.WaitForExit(10000)) { throw 'Uninstall did not close the running app.' }
        if (Test-Path -LiteralPath $uninstallKey) { throw 'The uninstall registration was not removed.' }
        if (Test-Path -LiteralPath $executable) { throw 'The installed executable was not removed.' }
        Write-Host 'Uninstall while running passed.'
        $installationDirectory = $null
    }
}
finally {
    if ($primaryProcess) {
        $primaryProcess.Refresh()
        if (-not $primaryProcess.HasExited) {
            $shutdownProcess = Invoke-HiddenProcess -File $executable -Arguments @('--quit')
            [void]$shutdownProcess.WaitForExit(15000)
            $shutdownProcess.Dispose()
            if (-not $primaryProcess.WaitForExit(5000)) {
                # Only terminate the process created by this test, never an unrelated instance.
                Stop-Process -Id $primaryProcess.Id -ErrorAction Continue
            }
        }
        $primaryProcess.Dispose()
    }

    if ($installationDirectory) {
        $uninstaller = Join-Path $installationDirectory 'unins000.exe'
        if (Test-Path -LiteralPath $uninstaller) {
            Wait-Success (Invoke-HiddenProcess -File $uninstaller -Arguments @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART'))
            if (Test-Path -LiteralPath $uninstallKey) { throw 'The test uninstall registration was not removed.' }
            Write-Host 'Test installation uninstalled successfully.'
        }
    }
    $env:DOTNET_ROOT = $originalDotnetRoot
    $env:DOTNET_MULTILEVEL_LOOKUP = $originalMultilevelLookup
}
