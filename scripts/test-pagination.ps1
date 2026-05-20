$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
$entry = 'https://web.archive.org/web/2/https://t.me/s/ProxyMTProto'
Write-Host "=== Page 1 ==="
$r1 = Invoke-WebRequest -Uri $entry -UseBasicParsing -TimeoutSec 45 -UserAgent $ua
$h1 = $r1.Content
$prev = [regex]::Match($h1, '<link\s+rel="prev"\s+href="([^"]+)"').Groups[1].Value
$snap = [regex]::Match($h1, '/web/(\d{14})').Groups[1].Value
$before = [regex]::Match($h1, 'before=(\d+)').Groups[1].Value
Write-Host "snapshot:" $snap "before:" $before
Write-Host "rel prev:" $prev
$urls = @(
    "https://web.archive.org$prev",
    "https://web.archive.org/web/2/https://t.me/s/ProxyMTProto?before=$before",
    "https://web.archive.org/web/${snap}if_/https://t.me/s/ProxyMTProto?before=$before",
    "https://web.archive.org/web/$snap/https://t.me/s/ProxyMTProto?before=$before"
)
Start-Sleep -Seconds 2
foreach ($u in $urls) {
    Write-Host "`n--- try ---"
    Write-Host $u
    try {
        $r = Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 45 -UserAgent $ua -Headers @{ Referer = $entry }
        $posts = ([regex]::Matches($r.Content, 'data-post')).Count
        $proxy = ([regex]::Matches($r.Content, '/proxy\?')).Count
        Write-Host "OK len=$($r.Content.Length) posts=$posts proxy=$proxy"
    } catch {
        Write-Host "FAIL:" $_.Exception.Message
    }
    Start-Sleep -Seconds 2
}
