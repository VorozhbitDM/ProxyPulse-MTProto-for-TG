$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$cs = Join-Path $root 'GenAppIcon.cs'
$exe = Join-Path $env:TEMP 'GenAppIcon.exe'
$source = Join-Path $root '..\src\ProxyPulse\app.png'
$out = Join-Path $root '..\src\ProxyPulse\app.ico'

if (-not (Test-Path $source)) {
    throw "Place icon at src\ProxyPulse\app.png"
}

$csc = "${env:WINDIR}\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { throw "csc not found" }

& $csc /nologo /out:$exe $cs
& $exe $source $out
Remove-Item $exe -Force -ErrorAction SilentlyContinue
Write-Host "Icon ready: $out"
