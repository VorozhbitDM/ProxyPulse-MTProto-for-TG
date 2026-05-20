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

$dotnet = Get-DotNetCli
if ($dotnet) {
    Write-Host "Building with .NET SDK: $dotnet"
    & $dotnet build $project -c Release
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

Copy-Item $outExe (Join-Path $dist "ProxyPulse.exe") -Force

Write-Host "Done: $dist\ProxyPulse.exe (single file, .NET Framework 4.7.2+ required)"
