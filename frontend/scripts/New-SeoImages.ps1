[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$frontendRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Split-Path -Parent $frontendRoot
$publicRoot = Join-Path $frontendRoot 'public'
$icon = [Drawing.Icon]::new((Join-Path $projectRoot 'logo.ico'), 256, 256)
$featureImage = [Drawing.Bitmap]::new(1200, 675)
$featureGraphics = [Drawing.Graphics]::FromImage($featureImage)
$logoImage = [Drawing.Bitmap]::new(512, 512)
$logoGraphics = [Drawing.Graphics]::FromImage($logoImage)
$ink = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#1E2935'))
$blue = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#2477C7'))
$muted = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#61717E'))
$line = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#D8DFE5'), 1)
$brandFont = [Drawing.Font]::new('Segoe UI', 28, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
$headlineFont = [Drawing.Font]::new('Segoe UI', 66, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
$bodyFont = [Drawing.Font]::new('Segoe UI', 24, [Drawing.FontStyle]::Regular, [Drawing.GraphicsUnit]::Pixel)
$smallFont = [Drawing.Font]::new('Segoe UI', 19, [Drawing.FontStyle]::Regular, [Drawing.GraphicsUnit]::Pixel)

try {
    foreach ($graphics in @($featureGraphics, $logoGraphics)) {
        $graphics.Clear([Drawing.ColorTranslator]::FromHtml('#F8FAFB'))
        $graphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    }

    # Keep the original icon and its square aspect ratio in both web assets.
    $logoGraphics.DrawIcon($icon, [Drawing.Rectangle]::new(88, 88, 336, 336))
    $logoImage.Save((Join-Path $publicRoot 'drink-water-logo.png'), [Drawing.Imaging.ImageFormat]::Png)

    $featureGraphics.DrawIcon($icon, [Drawing.Rectangle]::new(68, 56, 48, 48))
    $featureGraphics.DrawString('Drink Water.', $brandFont, $ink, 126, 59)
    $featureGraphics.DrawString('WINDOWS 10 / 11', $smallFont, $muted, 950, 69)
    $featureGraphics.DrawString('A little nudge.', $headlineFont, $ink, 68, 171)
    $featureGraphics.DrawString('A better routine.', $headlineFont, $blue, 68, 250)
    $featureGraphics.DrawString('Free water reminders for your Windows tray.', $bodyFont, $muted, 72, 346)
    $featureGraphics.DrawIcon($icon, [Drawing.Rectangle]::new(944, 194, 160, 160))
    $featureGraphics.DrawLine($line, 72, 421, 1128, 421)

    $featureGraphics.DrawString('10m / 30m / 1h', $brandFont, $ink, 72, 460)
    $featureGraphics.DrawString('Or set your own interval', $smallFont, $muted, 73, 505)
    $featureGraphics.DrawString('Your words.', $brandFont, $ink, 455, 460)
    $featureGraphics.DrawString('Editable reminder slogans', $smallFont, $muted, 456, 505)
    $featureGraphics.DrawString('Your routine.', $brandFont, $ink, 840, 460)
    $featureGraphics.DrawString('Optional Windows startup', $smallFont, $muted, 841, 505)

    $featureGraphics.DrawString('drinkwater.hardikgarg.me', $smallFont, $blue, 73, 608)
    $featureGraphics.DrawString('Free & open source', $smallFont, $muted, 966, 608)
    $featureImage.Save((Join-Path $publicRoot 'drink-water-features.png'), [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $smallFont.Dispose()
    $bodyFont.Dispose()
    $headlineFont.Dispose()
    $brandFont.Dispose()
    $line.Dispose()
    $muted.Dispose()
    $blue.Dispose()
    $ink.Dispose()
    $logoGraphics.Dispose()
    $logoImage.Dispose()
    $featureGraphics.Dispose()
    $featureImage.Dispose()
    $icon.Dispose()
}
