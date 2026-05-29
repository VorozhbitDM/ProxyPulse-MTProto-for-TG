$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0'
$html = (Invoke-WebRequest -Uri 'https://tgstat.com/channel/@ProxyMTProto' -UseBasicParsing -TimeoutSec 25 -UserAgent $ua).Content
Write-Host "len $($html.Length)"
Write-Host "before= count $(([regex]::Matches($html, 'before=')).Count)"
Write-Host "offset count $(([regex]::Matches($html, 'offset')).Count)"
Write-Host "load-more count $(([regex]::Matches($html, 'load-more|load_more|show-more|show_more', 'IgnoreCase')).Count)"
$m = [regex]::Match($html, 'data-next-url="([^"]+)"')
if ($m.Success) { Write-Host "data-next-url $($m.Groups[1].Value)" }
$m2 = [regex]::Match($html, 'href="(/channel/@ProxyMTProto[^"]*(?:before|offset|page)[^"]*)"')
if ($m2.Success) { Write-Host "href $($m2.Groups[1].Value)" }
$m3 = [regex]::Match($html, 'Show more[\s\S]{0,400}')
if ($m3.Success) { Write-Host $m3.Value.Substring(0, [Math]::Min(400, $m3.Value.Length)) }

# t.me first page before id
$t = (Invoke-WebRequest -Uri 'https://t.me/s/ProxyMTProto' -UseBasicParsing -TimeoutSec 25 -UserAgent $ua).Content
$posts = [regex]::Matches($t, 'data-post="ProxyMTProto/(\d+)"')
$min = [int]::MaxValue
foreach ($pm in $posts) { $id = [int]$pm.Groups[1].Value; if ($id -lt $min) { $min = $id } }
Write-Host "t.me min post id $min"
$url2 = "https://t.me/s/ProxyMTProto?before=$min"
$t2 = (Invoke-WebRequest -Uri $url2 -UseBasicParsing -TimeoutSec 25 -UserAgent $ua).Content
Write-Host "t.me page2 posts $(([regex]::Matches($t2, 'data-post=')).Count) proxy links $(([regex]::Matches($t2, 'Server:')).Count)"
