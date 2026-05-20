# Verifies archive.org feed pagination before changing ProxyPulse code.
$ErrorActionPreference = 'Continue'
$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
$entry = 'https://web.archive.org/web/2/https://t.me/s/ProxyMTProto'

function Get-ProxyCount($html) {
    return ([regex]::Matches($html, '(?:tg://proxy\?|/proxy\?|t\.me/proxy\?)')).Count
}

function Get-MinPostId($html) {
    $min = [long]::MaxValue
    foreach ($m in [regex]::Matches($html, 'data-post="[^"]+/(\d+)"')) {
        $id = [long]$m.Groups[1].Value
        if ($id -lt $min) { $min = $id }
    }
    if ($min -eq [long]::MaxValue) { return $null }
    return $min
}

function Try-Download($url, $referer) {
    try {
        $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 60 -UserAgent $ua -Headers @{
            Referer = $referer
            Accept = 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8'
        }
        return @{ Ok = $true; Html = $r.Content; Len = $r.Content.Length; Status = $r.StatusCode }
    } catch {
        return @{ Ok = $false; Error = $_.Exception.Message }
    }
}

function Build-StarUrl($snap, $before) {
    return "https://web.archive.org/web/$snap*/https://t.me/s/ProxyMTProto?before=$before"
}

function Build-RelStar($relPath) {
    if ($relPath -match '/web/(\d{14})') {
        $snap = $Matches[1]
        if ($relPath -match 'before=(\d+)') {
            $before = $Matches[1]
            return Build-StarUrl $snap $before
        }
    }
    return $null
}

Write-Host "========== TEST A: */ chain (3 pages) =========="
$referer = $entry
$p1 = Try-Download $entry 'https://web.archive.org/'
if (-not $p1.Ok) { Write-Host "PAGE1 FAIL:" $p1.Error; exit 1 }
$before1 = Get-MinPostId $p1.Html
$snap = [regex]::Match($p1.Html, '/web/(\d{14})').Groups[1].Value
Write-Host "P1 OK len=$($p1.Len) proxies=$([int](Get-ProxyCount $p1.Html)) minPost=$before1 snap=$snap"
Start-Sleep -Seconds 2

$pagesOk = 1
$prevBefore = $before1
$referer = $entry
for ($i = 2; $i -le 5; $i++) {
    $rel = [regex]::Match($p1.Html, '<link\s+rel="prev"\s+href="([^"]+)"').Groups[1].Value
    if ($i -gt 2) {
        $rel = [regex]::Match($script:lastHtml, '<link\s+rel="prev"\s+href="([^"]+)"').Groups[1].Value
    }
    $star = Build-RelStar $rel
    if (-not $star) { $star = Build-StarUrl $snap $prevBefore }
    Write-Host "P${i} try star: $star"
    Start-Sleep -Seconds 2
    $px = Try-Download $star $referer
    if (-not $px.Ok) {
        Write-Host "P${i} FAIL:" $px.Error
        break
    }
    $minP = Get-MinPostId $px.Html
    $cnt = Get-ProxyCount $px.Html
    Write-Host "P${i} OK len=$($px.Len) proxies=$cnt minPost=$minP (was before cursor $prevBefore)"
    if ($null -eq $minP -or $minP -ge $prevBefore) {
        Write-Host "P${i} WARN: cursor did not advance"
    }
    $script:lastHtml = $px.Html
    $referer = $star
    $prevBefore = $minP
    $pagesOk++
}

Write-Host "`n========== TEST B: CDX newest 5 snapshots (no before=) =========="
$cdx = Try-Download 'https://web.archive.org/cdx/search/cdx?url=t.me/s/ProxyMTProto&output=text&fl=timestamp&collapse=timestamp&limit=30' 'https://web.archive.org/'
if ($cdx.Ok) {
    $ids = @($cdx.Html -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -match '^\d{14}$' })
    $newest = $ids | Select-Object -Last 5
    $uniqPosts = @{}
    foreach ($t in $newest) {
        Start-Sleep -Seconds 2
        $u = "https://web.archive.org/web/${t}if_/https://t.me/s/ProxyMTProto"
        $r = Try-Download $u 'https://web.archive.org/'
        if ($r.Ok) {
            $c = Get-ProxyCount $r.Html
            Write-Host "CDX $t OK proxies=$c len=$($r.Len)"
        } else {
            Write-Host "CDX $t FAIL $($r.Error)"
        }
    }
}

Write-Host "`n========== SUMMARY =========="
Write-Host "*/ chain pages OK: $pagesOk"
