$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0'

function Count-Proxies($html) {
    $keys = New-Object System.Collections.Generic.HashSet[string]
    foreach ($m in [regex]::Matches($html, 'Server:\s*(?:<[^>]+>)*\s*(.+?)\s*(?:<[^>]+>)*\s*Port:\s*(?:<[^>]+>)*\s*(\d+)\s*(?:<[^>]+>)*\s*Secret:\s*(?:<[^>]+>)*\s*([0-9a-fA-F+/=]+)', 'IgnoreCase')) {
        $s = ($m.Groups[1].Value -replace '<[^>]+>','').Trim()
        $keys.Add("$s`:$($m.Groups[2].Value):$($m.Groups[3].Value)") | Out-Null
    }
    foreach ($m in [regex]::Matches($html, 'server=([^&\s""''<>]+)&port=(\d+)&secret=([0-9a-fA-F+/=]+)', 'IgnoreCase')) {
        $keys.Add("$($m.Groups[1].Value):$($m.Groups[2].Value):$($m.Groups[3].Value)") | Out-Null
    }
    return $keys.Count
}

foreach ($url in @('https://tgstat.com/channel/@ProxyMTProto', 'https://tgstat.ru/channel/@ProxyMTProto')) {
    $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30 -UserAgent $ua
    Write-Host "$url unique $($r.Content.Length) bytes proxies $(Count-Proxies $r.Content)"
}

# archive first page + before pagination
$arch = Invoke-WebRequest -Uri 'https://web.archive.org/web/2/https://t.me/s/ProxyMTProto' -UseBasicParsing -TimeoutSec 40 -UserAgent $ua
Write-Host "archive page1 proxies $(Count-Proxies $arch.Content)"
$min = [long]::MaxValue
foreach ($pm in [regex]::Matches($arch.Content, 'data-post="[^/]+/(\d+)"')) {
    $id = [long]$pm.Groups[1].Value
    if ($id -lt $min) { $min = $id }
}
Write-Host "archive min post $min"
$before = [regex]::Match($arch.Content, 'ProxyMTProto\?before=(\d+)').Groups[1].Value
Write-Host "rel prev before=$before"
if ($before) {
    $arch2 = Invoke-WebRequest -Uri "https://web.archive.org/web/2/https://t.me/s/ProxyMTProto?before=$before" -UseBasicParsing -TimeoutSec 40 -UserAgent $ua
    Write-Host "archive page2 proxies $(Count-Proxies $arch2.Content)"
}
