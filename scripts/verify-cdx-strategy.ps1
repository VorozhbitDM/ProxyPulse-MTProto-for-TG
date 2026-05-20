# Verified strategy: page1 feed + CDX snapshots (no ?before= pagination)
$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
$keys = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
$total = 0

function Add-FromHtml($html) {
    $added = 0
    foreach ($m in [regex]::Matches($html, '(?:tg://proxy\?|/proxy\?|t\.me/proxy\?)([^"''\s<>]+)')) {
        $q = $m.Groups[1].Value
        if ($keys.Add($q)) { $script:added++; $script:total++ }
    }
    return $added
}

Write-Host "=== Page 1 feed ==="
$r1 = Invoke-WebRequest -Uri 'https://web.archive.org/web/2/https://t.me/s/ProxyMTProto' -UseBasicParsing -TimeoutSec 60 -UserAgent $ua
$a1 = Add-FromHtml $r1.Content
Write-Host "+$a1 unique total=$total"

Write-Host "`n=== CDX newest 25 ==="
$cdx = Invoke-WebRequest -Uri 'https://web.archive.org/cdx/search/cdx?url=t.me/s/ProxyMTProto&output=text&fl=timestamp&collapse=timestamp&limit=80' -UseBasicParsing -TimeoutSec 45 -UserAgent $ua
$ids = @($cdx.Content -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -match '^\d{14}$' })
$newest = $ids | Select-Object -Last 25 | Sort-Object -Descending
$ok = 0
foreach ($t in $newest) {
    Start-Sleep -Milliseconds 1200
    try {
        $u = "https://web.archive.org/web/${t}if_/https://t.me/s/ProxyMTProto"
        $r = Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 45 -UserAgent $ua -Headers @{ Referer = 'https://web.archive.org/' }
        $dp = ([regex]::Matches($r.Content, 'data-post')).Count
        if ($dp -eq 0) { Write-Host "$t EMPTY"; continue }
        $a = Add-FromHtml $r.Content
        $ok++
        Write-Host "$t +$a (unique total=$total)"
        if ($total -ge 100) { Write-Host "STOP at 100"; break }
    } catch {
        Write-Host "$t FAIL"
    }
}
Write-Host "`nRESULT: $ok snapshots OK, $total unique proxies (target 100)"
