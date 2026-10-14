[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$frontendRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Split-Path -Parent $frontendRoot
$bitmap = [Drawing.Bitmap]::new(1200, 630)
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$ink = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#1E2935'))
$blue = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#2477C7'))
$muted = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#61717E'))
$softBlue = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#E7F0F7'))
$brandFont = [Drawing.Font]::new('Segoe UI', 28, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
$headlineFont = [Drawing.Font]::new('Segoe UI', 66, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
$bodyFont = [Drawing.Font]::new('Segoe UI', 24, [Drawing.FontStyle]::Regular, [Drawing.GraphicsUnit]::Pixel)
$smallFont = [Drawing.Font]::new('Segoe UI', 19, [Drawing.FontStyle]::Regular, [Drawing.GraphicsUnit]::Pixel)
$icon = [Drawing.Icon]::new((Join-Path $projectRoot 'logo.ico'), 256, 256)
try {
    $graphics.Clear([Drawing.ColorTranslator]::FromHtml('#F8FAFB'))
    $graphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.DrawIcon($icon, [Drawing.Rectangle]::new(68, 60, 48, 48))
    $graphics.DrawString('Drink Water.', $brandFont, $ink, 126, 63)
    $graphics.DrawString('Your next sip,', $headlineFont, $ink, 68, 185)
    $graphics.DrawString('right on time.', $headlineFont, $blue, 68, 265)
    $graphics.DrawString('A free water reminder for your Windows tray.', $bodyFont, $muted, 72, 380)
    $graphics.DrawString('Custom intervals  ·  Your slogans  ·  Optional startup', $smallFont, $muted, 73, 439)
    $graphics.DrawString('drinkwater.hardikgarg.me', $smallFont, $blue, 73, 547)
    $graphics.FillEllipse($softBlue, 845, 154, 280, 280)
    $graphics.DrawIcon($icon, [Drawing.Rectangle]::new(906, 191, 156, 206))
    $graphics.DrawString('Windows 10 / 11', $smallFont, $muted, 906, 460)
    $bitmap.Save((Join-Path $frontendRoot 'public/social-card.png'), [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $icon.Dispose()
    $smallFont.Dispose()
    $bodyFont.Dispose()
    $headlineFont.Dispose()
    $brandFont.Dispose()
    $softBlue.Dispose()
    $muted.Dispose()
    $blue.Dispose()
    $ink.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}
