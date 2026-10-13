[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Build-Environment.ps1')

Push-Location $windowsRoot
try {
    Invoke-Dotnet -Arguments @('restore', 'DrinkWater.sln')
    Invoke-Dotnet -Arguments @('build', 'DrinkWater.sln', '--configuration', 'Release', '--no-restore')
    Invoke-Dotnet -Arguments @('format', 'DrinkWater.sln', '--verify-no-changes', '--no-restore')
    Invoke-Dotnet -Arguments @('test', 'DrinkWater.sln', '--configuration', 'Release', '--no-build', '--no-restore', '--logger', 'console;verbosity=minimal')
}
finally {
    Pop-Location
}
