Add-Type -AssemblyName System.Drawing

$w = 280
$h = 56
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

$rect = New-Object System.Drawing.Rectangle 0, 0, $w, $h
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, `
    ([System.Drawing.Color]::FromArgb(255, 155, 92, 255)), `
    ([System.Drawing.Color]::FromArgb(255, 123, 63, 242)), `
    45.0
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$radius = 12
$path.AddArc(0, 0, $radius * 2, $radius * 2, 180, 90)
$path.AddArc($w - $radius * 2, 0, $radius * 2, $radius * 2, 270, 90)
$path.AddArc($w - $radius * 2, $h - $radius * 2, $radius * 2, $radius * 2, 0, 90)
$path.AddArc(0, $h - $radius * 2, $radius * 2, $radius * 2, 90, 90)
$path.CloseFigure()
$g.FillPath($brush, $path)

$white = [System.Drawing.Brushes]::White
$g.FillEllipse($white, 10, 12, 32, 32)

$fontRub = New-Object System.Drawing.Font 'Segoe UI', 16, ([System.Drawing.FontStyle]::Bold)
$fontTitle = New-Object System.Drawing.Font 'Segoe UI', 13, ([System.Drawing.FontStyle]::Bold)
$fontSub = New-Object System.Drawing.Font 'Segoe UI', 11, ([System.Drawing.FontStyle]::Regular)
$purple = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 123, 63, 242))
$light = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 239, 230, 255))
$sf = New-Object System.Drawing.StringFormat
$sf.Alignment = [System.Drawing.StringAlignment]::Center
$sf.LineAlignment = [System.Drawing.StringAlignment]::Center

$g.DrawString([char]0x20BD, $fontRub, $purple, (New-Object System.Drawing.RectangleF 10, 12, 32, 32), $sf)
$g.DrawString([string][char]0x041F + [char]0x043E + [char]0x0434 + [char]0x0434 + [char]0x0435 + [char]0x0440 + [char]0x0436 + [char]0x0430 + [char]0x0442 + [char]0x044C, $fontTitle, $white, (New-Object System.Drawing.RectangleF 52, 8, 220, 24), $sf)
$g.DrawString([string][char]0x042E + 'Money', $fontSub, $light, (New-Object System.Drawing.RectangleF 52, 30, 220, 22), $sf)

$out = Join-Path $PSScriptRoot '..\docs\yoomoney-support.png'
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Wrote $out ($((Get-Item $out).Length) bytes)"

$g.Dispose()
$bmp.Dispose()
