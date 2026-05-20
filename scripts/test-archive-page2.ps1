$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
$ref = 'https://web.archive.org/web/2/https://t.me/s/ProxyMTProto'
$urls = @(
  'https://web.archive.org/web/20260519104022/https://t.me/s/ProxyMTProto?before=47473',
  'https://web.archive.org/web/20260519104022if_/https://t.me/s/ProxyMTProto?before=47473'
)
Start-Sleep -Seconds 2
foreach ($u in $urls) {
  try {
    $r = Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 45 -UserAgent $ua -Headers @{ Referer = $ref }
    $posts = ([regex]::Matches($r.Content, 'data-post="[^"]+/(\d+)"')).Count
    Write-Host "OK len=$($r.Content.Length) posts=$posts"
  } catch {
    Write-Host "FAIL $($_.Exception.Message)"
  }
  Start-Sleep -Seconds 2
}
