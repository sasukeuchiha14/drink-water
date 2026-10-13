[CmdletBinding()]
param(
    [switch] $SkipInstaller,
    [string] $CompilerPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Build-Environment.ps1')

Push-Location $windowsRoot
try {
    $publishDirectory = Join-Path $windowsRoot 'artifacts/publish/win-x64'
    [xml] $projectProperties = Get-Content -LiteralPath (Join-Path $windowsRoot 'Directory.Build.props')
    $appVersion = [string] $projectProperties.Project.PropertyGroup.Version
    Invoke-Dotnet -Arguments @('publish', 'src/DrinkWater.App/DrinkWater.App.csproj', '--configuration', 'Release', '--runtime', 'win-x64', '--self-contained', 'true', '--output', $publishDirectory, '-p:PublishTrimmed=false', '-p:DebugType=none', '-p:DebugSymbols=false')
    $noticesDirectory = Join-Path $publishDirectory 'Notices'
    New-Item -ItemType Directory -Path $noticesDirectory -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $windowsRoot 'LICENSE.txt') -Destination (Join-Path $publishDirectory 'LICENSE.txt') -Force
    $runtimeConfiguration = Get-Content -LiteralPath (Join-Path $publishDirectory 'DrinkWater.runtimeconfig.json') -Raw | ConvertFrom-Json
    foreach ($framework in $runtimeConfiguration.runtimeOptions.includedFrameworks) {
        if ($framework.name -eq 'Microsoft.NETCore.App') {
            $packageDirectory = Join-Path $env:NUGET_PACKAGES ('microsoft.netcore.app.runtime.win-x64/' + $framework.version)
            Copy-Item -LiteralPath (Join-Path $packageDirectory 'LICENSE.TXT') -Destination (Join-Path $noticesDirectory 'DotNetRuntime-LICENSE.txt') -Force
            Copy-Item -LiteralPath (Join-Path $packageDirectory 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $noticesDirectory 'DotNetRuntime-THIRD-PARTY-NOTICES.txt') -Force
        }
        elseif ($framework.name -eq 'Microsoft.WindowsDesktop.App') {
            $packageDirectory = Join-Path $env:NUGET_PACKAGES ('microsoft.windowsdesktop.app.runtime.win-x64/' + $framework.version)
            Copy-Item -LiteralPath (Join-Path $packageDirectory 'LICENSE') -Destination (Join-Path $noticesDirectory 'DotNetDesktop-LICENSE.txt') -Force
        }
    }
    if (-not $SkipInstaller) {
        if (-not $CompilerPath) {
            $candidates = @(
                (Join-Path $windowsRoot '.tools/inno/ISCC.exe'),
                'C:/Program Files/Inno Setup 7/ISCC.exe',
                'C:/Program Files (x86)/Inno Setup 6/ISCC.exe'
            )
            $CompilerPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
            if (-not $CompilerPath) {
                $compilerCommand = Get-Command ISCC -ErrorAction SilentlyContinue
                if ($compilerCommand) { $CompilerPath = $compilerCommand.Source }
            }
        }
        if (-not $CompilerPath) { throw 'Inno Setup was not found. Run scripts/Get-BuildTools.ps1 or pass -CompilerPath.' }
        & $CompilerPath '/Q' ("/DPublishDir=$publishDirectory") ("/DAppVersion=$appVersion") (Join-Path $windowsRoot 'installer/DrinkWater.iss')
        if ($LASTEXITCODE -ne 0) { throw ('Installer compilation failed: ' + $LASTEXITCODE) }
        Write-Host ('Installer created in ' + (Join-Path $windowsRoot 'artifacts/installer'))
    }
}
finally {
    Pop-Location
}
