$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $root "src\ProxyPulse"
$project = Join-Path $projectDir "ProxyPulse.csproj"
$dist = Join-Path $root "dist"
$packagesConfig = Join-Path $projectDir "packages.config"
$packagesDir = Join-Path $root "packages"
$nuget = Join-Path $root "tools\nuget.exe"

# Refresh PATH (dotnet SDK if installed after Cursor started)
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" +
            [System.Environment]::GetEnvironmentVariable("Path", "User")

function Get-DotNetCli {
    $candidates = @(
        "dotnet",
        "${env:ProgramFiles}\dotnet\dotnet.exe",
        "${env:ProgramFiles(x86)}\dotnet\dotnet.exe"
    )
    foreach ($c in $candidates) {
        if ($c -eq "dotnet") {
            $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
            if ($cmd) { return $cmd.Source }
        }
        elseif (Test-Path $c) { return $c }
    }
    return $null
}

$iconScript = Join-Path $root "scripts\gen-app-icon.ps1"
if (Test-Path $iconScript) {
    Write-Host "Updating app.ico from src\ProxyPulse\app.png..."
    & powershell -NoProfile -ExecutionPolicy Bypass -File $iconScript
}

Write-Host "Clean rebuild (icon embed)..."
$dotnet = Get-DotNetCli
if ($dotnet) {
    & $dotnet clean $project -c Release -v q | Out-Null
    Remove-Item -Recurse -Force (Join-Path $projectDir "bin"), (Join-Path $projectDir "obj") -ErrorAction SilentlyContinue
}

$dotnet = Get-DotNetCli
if ($dotnet) {
    Write-Host "Building with .NET SDK: $dotnet"
    & $dotnet build $project -c Release --no-incremental
    $candidates = @(
        (Join-Path $projectDir "bin\Release\net472\ProxyPulse.exe"),
        (Join-Path $projectDir "bin\Release\ProxyPulse.exe")
    )
    $outExe = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
else {
    Write-Host "Building with MSBuild + NuGet (no .NET SDK in PATH)..."

    if (-not (Test-Path $nuget)) {
        New-Item -ItemType Directory -Path (Split-Path $nuget) -Force | Out-Null
        Write-Host "Downloading nuget.exe..."
        Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nuget -UseBasicParsing
    }

    & $nuget restore $packagesConfig -PackagesDirectory $packagesDir -NonInteractive

    $msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
    if (-not (Test-Path $msbuild)) {
        throw "MSBuild not found at $msbuild"
    }

    & $msbuild $project /p:Configuration=Release /v:minimal
    $outExe = Join-Path $projectDir "bin\Release\ProxyPulse.exe"
}

if (-not (Test-Path $outExe)) {
    throw "Build failed: $outExe not found"
}

if (Test-Path $dist) {
    Remove-Item (Join-Path $dist "*") -Force -ErrorAction SilentlyContinue
}
else {
    New-Item -ItemType Directory -Path $dist | Out-Null
}

$distExe = Join-Path $dist "ProxyPulse.exe"
Copy-Item $outExe $distExe -Force
# Touch dist so Explorer shows updated time after icon/embed changes
(Get-Item $distExe).LastWriteTime = Get-Date

# Release ZIP: only ProxyPulse.exe (do not zip bin\Release — there are .pdb, .config, etc.)
$version = "2.6"
if (Test-Path $project) {
    [xml]$csproj = Get-Content $project
    $verText = ($csproj.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
    if ($verText) {
        $v = [version]$verText
        $version = if ($v.Build -eq 0) { "$($v.Major).$($v.Minor)" } else { "$($v.Major).$($v.Minor).$($v.Build)" }
    }
}
$zipName = "ProxyPulse-v$version-win-x64.zip"
$zipPath = Join-Path $dist $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path $distExe -DestinationPath $zipPath -CompressionLevel Optimal

$hash = (Get-FileHash $distExe -Algorithm SHA256).Hash.Substring(0, 16)
Write-Host "Done: $distExe"
Write-Host "      SHA256: $hash...  Size: $((Get-Item $distExe).Length) bytes"
Write-Host "ZIP:  $zipPath (only ProxyPulse.exe)"
Write-Host "      Upload this file to GitHub Release Assets."

# Preview embedded icon (for verification; Explorer may still show cached icon)
try {
    Add-Type -AssemblyName System.Drawing
    $ico = [System.Drawing.Icon]::ExtractAssociatedIcon($distExe)
    if ($ico) {
        $preview = Join-Path $dist "icon-embedded-preview.png"
        $ico.ToBitmap().Save($preview, [System.Drawing.Imaging.ImageFormat]::Png)
        $ico.Dispose()
        Write-Host "Preview: $preview (open this if Explorer shows old icon)"
    }
} catch { }

if (Get-Command ie4uinit.exe -ErrorAction SilentlyContinue) {
    ie4uinit.exe -show | Out-Null
}
Write-Host ""
Write-Host "If dist\ProxyPulse.exe still shows the OLD icon in Explorer, run:"
Write-Host "  .\scripts\refresh-shell-icons.ps1"
