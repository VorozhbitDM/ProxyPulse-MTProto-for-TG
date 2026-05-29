$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0'
$html = (Invoke-WebRequest -Uri 'https://tgstat.com/channel/@ProxyMTProto' -UseBasicParsing -TimeoutSec 30 -UserAgent $ua).Content
$idx = $html.IndexOf('lm-page')
if ($idx -ge 0) { $html.Substring([Math]::Max(0,$idx-500), 1500) | Out-File 'd:\AI\MTProto\scripts\tgstat-lm-snippet.txt' -Encoding utf8 }
Select-String -InputObject $html -Pattern 'lm-[a-z-]+|loadMore|/channel/@ProxyMTProto[^"\s]*' -AllMatches | ForEach-Object { $_.Matches } | Select-Object -First 40 Value | Out-File 'd:\AI\MTProto\scripts\tgstat-lm-matches.txt' -Encoding utf8
Write-Host 'done'
