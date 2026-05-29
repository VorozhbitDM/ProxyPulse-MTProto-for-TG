$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$cs = Join-Path $root 'GenAppIcon.cs'
$exe = Join-Path $env:TEMP 'GenAppIcon.exe'
$out = Join-Path $root '..\src\ProxyPulse\app.ico'
$repoRoot = Split-Path -Parent $root

$candidates = @(
    (Join-Path $repoRoot 'android\Newicon333.jpg'),
    (Join-Path $repoRoot 'android\Newicon333.png'),
    (Join-Path $repoRoot 'android\icon.png'),
    (Join-Path $root '..\src\ProxyPulse\app.png')
)

$source = $null
foreach ($candidate in $candidates) {
    if (Test-Path $candidate) {
        $source = (Resolve-Path $candidate).Path
        break
    }
}

if (-not $source) {
    throw "Place icon at android\Newicon333.jpg or src\ProxyPulse\app.png"
}

$csc = "${env:WINDIR}\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { throw "csc not found" }

& $csc /nologo /out:$exe $cs
& $exe $source $out 0.99
Remove-Item $exe -Force -ErrorAction SilentlyContinue
Write-Host "Icon ready: $out"
