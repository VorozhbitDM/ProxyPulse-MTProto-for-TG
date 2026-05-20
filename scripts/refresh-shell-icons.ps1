# Refreshes Windows icon cache so Explorer shows updated exe icons.
$ErrorActionPreference = 'Stop'

Write-Host "Refreshing shell icon cache..."

if (Get-Command ie4uinit.exe -ErrorAction SilentlyContinue) {
    ie4uinit.exe -show | Out-Null
}

$local = "$env:LOCALAPPDATA\IconCache.db"
$localMs = "$env:LOCALAPPDATA\Microsoft\Windows\Explorer\iconcache*.db"

Get-Process explorer -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

if (Test-Path $local) { Remove-Item $local -Force -ErrorAction SilentlyContinue }
Get-ChildItem $localMs -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

Start-Process explorer.exe
Write-Host "Done. Open dist\ProxyPulse.exe or icon-embedded-preview.png to verify."
