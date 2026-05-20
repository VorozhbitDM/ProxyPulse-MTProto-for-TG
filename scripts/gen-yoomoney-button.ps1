Add-Type -AssemblyName System.Drawing

$btnW = 140
$btnH = 36
$radius = 8
$purple = [System.Drawing.Color]::FromArgb(255, 143, 69, 253)

$bmp = New-Object System.Drawing.Bitmap $btnW, $btnH
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$r = $radius
$path.AddArc(0, 0, $r * 2, $r * 2, 180, 90)
$path.AddArc($btnW - $r * 2, 0, $r * 2, $r * 2, 270, 90)
$path.AddArc($btnW - $r * 2, $btnH - $r * 2, $r * 2, $r * 2, 0, 90)
$path.AddArc(0, $btnH - $r * 2, $r * 2, $r * 2, 90, 90)
$path.CloseFigure()
$g.FillPath((New-Object System.Drawing.SolidBrush $purple), $path)

$font = New-Object System.Drawing.Font 'Segoe UI', 13, ([System.Drawing.FontStyle]::Bold)
$white = [System.Drawing.Brushes]::White
$sf = New-Object System.Drawing.StringFormat
$sf.Alignment = [System.Drawing.StringAlignment]::Center
$sf.LineAlignment = [System.Drawing.StringAlignment]::Center
$label = [string][char]0x042E + 'Money'
$g.DrawString($label, $font, $white, (New-Object System.Drawing.RectangleF 0, 0, $btnW, $btnH), $sf)

$out = Join-Path $PSScriptRoot '..\docs\yoomoney-support.png'
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Wrote $out ($btnW x $btnH)"

$font.Dispose()
$g.Dispose()
$bmp.Dispose()
