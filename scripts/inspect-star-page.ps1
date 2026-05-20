$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
$ref = 'https://web.archive.org/web/2/https://t.me/s/ProxyMTProto'
$u = 'https://web.archive.org/web/20260519104022*/https://t.me/s/ProxyMTProto?before=47473'
$r = Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 60 -UserAgent $ua -Headers @{ Referer = $ref }
$h = $r.Content
Write-Host "status" $r.StatusCode "len" $h.Length
Write-Host "data-post" ([regex]::Matches($h, 'data-post')).Count
Write-Host "tgme_widget_message" ([regex]::Matches($h, 'tgme_widget_message')).Count
Write-Host "proxy question" ([regex]::Matches($h, 'proxy\?')).Count
Write-Host "rel prev" ($h -match 'rel="prev"')
$m = [regex]::Match($h, 'rel="prev"\s+href="([^"]+)"')
if ($m.Success) { Write-Host "next prev:" $m.Groups[1].Value }
$out = 'd:\AI\MTProto\scripts\star-page-sample.html'
$h.Substring(0, [Math]::Min(8000, $h.Length)) | Out-File $out -Encoding utf8
Write-Host "wrote head to" $out
