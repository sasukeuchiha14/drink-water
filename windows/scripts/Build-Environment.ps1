$windowsRoot = Split-Path -Parent $PSScriptRoot
$localDotnet = Join-Path $windowsRoot '.tools/dotnet/dotnet.exe'
if (Test-Path -LiteralPath $localDotnet) {
    $dotnetExecutable = $localDotnet
    $env:DOTNET_ROOT = Split-Path -Parent $localDotnet
}
else {
    $dotnetCommand = Get-Command dotnet -ErrorAction Stop
    $dotnetExecutable = $dotnetCommand.Source
}
$env:DOTNET_CLI_HOME = Join-Path $windowsRoot '.tools/cli-home'
$env:NUGET_PACKAGES = Join-Path $windowsRoot '.tools/nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'

function Invoke-Dotnet {
    param([string[]] $Arguments)
    & $dotnetExecutable @Arguments
    if ($LASTEXITCODE -ne 0) { throw ('dotnet failed with exit code ' + $LASTEXITCODE) }
}
