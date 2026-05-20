$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
$ref = 'https://web.archive.org/web/2/https://t.me/s/ProxyMTProto'
$r1 = Invoke-WebRequest -Uri $ref -UseBasicParsing -TimeoutSec 60 -UserAgent $ua
$h1 = $r1.Content
$before = ([regex]::Match($h1, 'before=(\d+)')).Groups[1].Value
$snap = ([regex]::Match($h1, '/web/(\d{14})')).Groups[1].Value
$rel = ([regex]::Match($h1, 'rel="prev"\s+href="([^"]+)"')).Groups[1].Value
Write-Host "snap=$snap before=$before"
Write-Host "rel=$rel"

# iframe / embed hints on page 1
foreach ($pat in @('iframe[^>]+src="([^"]+)"', 'embed[^>]+src="([^"]+)"', 't\.me/s/ProxyMTProto[^"''<>]*')) {
    $ms = [regex]::Matches($h1, $pat)
    Write-Host "pattern $pat count=$($ms.Count)"
    foreach ($m in $ms | Select-Object -First 3) {
        $v = if ($m.Groups.Count -gt 1) { $m.Groups[1].Value } else { $m.Value }
        Write-Host "  " $v.Substring(0, [Math]::Min(100, $v.Length))
    }
}

$candidates = @(
    "https://web.archive.org$rel",
    "https://web.archive.org/web/${snap}if_/https://t.me/s/ProxyMTProto?before=$before",
    "https://web.archive.org/web/${snap}id_/https://t.me/s/ProxyMTProto?before=$before",
    "https://web.archive.org/web/${snap}/https://t.me/s/ProxyMTProto?before=$before",
    "https://web.archive.org/web/2im_/https://t.me/s/ProxyMTProto?before=$before"
)

Start-Sleep 3
foreach ($u in $candidates) {
    Write-Host "`n--- $u"
    try {
        $r = Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 60 -UserAgent $ua -Headers @{ Referer = $ref }
        $dp = ([regex]::Matches($r.Content, 'data-post')).Count
        $px = ([regex]::Matches($r.Content, 'proxy\?')).Count
        Write-Host "OK len=$($r.Content.Length) data-post=$dp proxy=$px"
        if ($dp -gt 0) {
            $np = [regex]::Match($r.Content, 'rel="prev"\s+href="([^"]+)"')
            if ($np.Success) { Write-Host "  next:" $np.Groups[1].Value }
        }
    } catch {
        Write-Host "FAIL $($_.Exception.Message)"
    }
    Start-Sleep 4
}
