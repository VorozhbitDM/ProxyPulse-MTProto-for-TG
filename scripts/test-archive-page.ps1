$url = 'https://web.archive.org/web/2/https://t.me/s/ProxyMTProto'
$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
$r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30 -UserAgent $ua
$h = $r.Content
Write-Host "Len:" $h.Length
Write-Host "rel prev:" ($h -match 'rel="prev"')
Write-Host "data-post count:" ([regex]::Matches($h, 'data-post="[^"]+/(\d+)"')).Count
$m = [regex]::Match($h, '<link\s+rel="prev"\s+href="([^"]+)"')
if ($m.Success) { Write-Host "prev href:" $m.Groups[1].Value }
$m2 = [regex]::Match($h, 'href="(/web/\d+/https://t\.me/s/ProxyMTProto\?before=\d+)"[^>]*class="[^"]*tme_messages_more')
if ($m2.Success) { Write-Host "more href:" $m2.Groups[1].Value }
$snap = [regex]::Match($h, '/web/(\d{14})/https://t\.me/s/ProxyMTProto')
if ($snap.Success) { Write-Host "snapshot:" $snap.Groups[1].Value }
