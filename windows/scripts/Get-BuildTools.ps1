[CmdletBinding()]
param([switch] $SkipSdk)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$windowsRoot = Split-Path -Parent $PSScriptRoot
$toolsRoot = Join-Path $windowsRoot '.tools'
$downloadsRoot = Join-Path $toolsRoot 'downloads'
$sdkRoot = Join-Path $toolsRoot 'dotnet'
$innoRoot = Join-Path $toolsRoot 'inno'
New-Item -ItemType Directory -Path $downloadsRoot -Force | Out-Null

if (-not $SkipSdk -and -not (Test-Path -LiteralPath (Join-Path $sdkRoot 'dotnet.exe'))) {
    Write-Host 'Finding the latest stable .NET 10 SDK from Microsoft...'
    $metadata = Invoke-RestMethod -Uri 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json' -TimeoutSec 60
    $release = $metadata.releases | Where-Object { $_.sdk.version -eq $metadata.'latest-sdk' } | Select-Object -First 1
    $sdk = $release.sdk.files | Where-Object { $_.rid -eq 'win-x64' -and $_.name -like '*.zip' } | Select-Object -First 1
    if (-not $sdk -or $sdk.url -notlike 'https://builds.dotnet.microsoft.com/*') {
        throw 'Microsoft did not return the expected Windows SDK download.'
    }

    $archive = Join-Path $downloadsRoot ('dotnet-sdk-' + $metadata.'latest-sdk' + '.zip')
    Write-Host ('Downloading .NET SDK ' + $metadata.'latest-sdk' + '...')
    Invoke-WebRequest -Uri $sdk.url -OutFile $archive -TimeoutSec 600
    $actualHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash
    if ($actualHash -ne $sdk.hash) { throw 'The .NET SDK SHA-512 checksum did not match Microsoft metadata.' }
    New-Item -ItemType Directory -Path $sdkRoot -Force | Out-Null
    Expand-Archive -LiteralPath $archive -DestinationPath $sdkRoot -Force
}

if (-not (Test-Path -LiteralPath (Join-Path $innoRoot 'ISCC.exe'))) {
    $installer = Join-Path $downloadsRoot 'innosetup-7.1.0-x64.exe'
    Write-Host 'Downloading the signed Inno Setup 7.1.0 compiler...'
    Invoke-WebRequest -Uri 'https://github.com/jrsoftware/issrc/releases/download/is-7_1_0/innosetup-7.1.0-x64.exe' -OutFile $installer -TimeoutSec 180
    $signature = Get-AuthenticodeSignature -LiteralPath $installer
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Pyrsys B\.V\.') {
        throw 'The Inno Setup publisher signature could not be verified.'
    }

    $installArguments = @('/PORTABLE=1', '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/CURRENTUSER', ('/DIR="' + $innoRoot + '"'))
    $installProcess = Start-Process -FilePath $installer -ArgumentList $installArguments -WindowStyle Hidden -Wait -PassThru
    if ($installProcess.ExitCode -ne 0) { throw ('Inno Setup extraction failed: ' + $installProcess.ExitCode) }
}

if (-not $SkipSdk) { Write-Host ('SDK: ' + (Join-Path $sdkRoot 'dotnet.exe')) }
Write-Host ('Installer compiler: ' + (Join-Path $innoRoot 'ISCC.exe'))
