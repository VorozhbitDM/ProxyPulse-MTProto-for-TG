$urls = @(
  'https://web.archive.org/cdx/search/cdx?url=t.me/s/ProxyMTProto&output=text&fl=timestamp&collapse=timestamp&limit=10',
  'https://web.archive.org/cdx/search/cdx?url=t.me/s/ProxyMTProto&output=json&fl=timestamp&filter=statuscode:200&collapse=digest&limit=10'
)
$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
foreach ($url in $urls) {
  Write-Host "---" $url.Substring(0, 60) "..."
  try {
    $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 40 -UserAgent $ua
    $n = ([regex]::Matches($r.Content, '\d{14}')).Count
    Write-Host "OK len=$($r.Content.Length) timestamps~=$n"
  } catch {
    Write-Host "FAIL:" $_.Exception.Message
  }
}
