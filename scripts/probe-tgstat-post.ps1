$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0'
$s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$base = 'https://tgstat.com/channel/@ProxyMTProto'
$r1 = Invoke-WebRequest -Uri $base -UseBasicParsing -TimeoutSec 30 -UserAgent $ua -WebSession $s
$html = $r1.Content
$csrf = [regex]::Match($html, 'name="_tgstat_csrk"\s+value="([^"]+)"').Groups[1].Value
$page = [regex]::Match($html, 'name="page"\s+value="([^"]+)"').Groups[1].Value
$offset = [regex]::Match($html, 'name="offset"\s+value="([^"]+)"').Groups[1].Value
$body = "_tgstat_csrk=$([uri]::EscapeDataString($csrf))&page=$page&offset=$offset&hideDeleted=1"
$r = Invoke-WebRequest -Uri "$base/posts-last" -Method POST -Body $body -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -TimeoutSec 30 -UserAgent $ua -WebSession $s -Headers @{
    Referer = $base
    Origin = 'https://tgstat.com'
    'X-Requested-With' = 'XMLHttpRequest'
}
$r.Content | Out-File "$PSScriptRoot\tgstat-ajax-response.txt" -Encoding utf8
Write-Host "response: $($r.Content)"

# paginate multiple times
$curPage = $page
$curOffset = [int]$offset
$totalServers = 0
for ($n = 1; $n -le 5; $n++) {
    $body = "_tgstat_csrk=$([uri]::EscapeDataString($csrf))&page=$curPage&offset=$curOffset&hideDeleted=1"
    $rx = Invoke-WebRequest -Uri "$base/posts-last" -Method POST -Body $body -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -TimeoutSec 30 -UserAgent $ua -WebSession $s -Headers @{
        Referer = $base
        Origin = 'https://tgstat.com'
        'X-Requested-With' = 'XMLHttpRequest'
    }
    $chunk = $rx.Content
    $cnt = ([regex]::Matches($chunk, 'Server:')).Count
    $totalServers += $cnt
    $np = [regex]::Match($chunk, 'name="page"\s+value="([^"]+)"').Groups[1].Value
    $no = [regex]::Match($chunk, 'name="offset"\s+value="([^"]+)"').Groups[1].Value
    Write-Host "batch $n len $($chunk.Length) servers $cnt next page=$np offset=$no"
    if ($chunk.Length -lt 1000 -and $cnt -eq 0) { Write-Host "short: $chunk"; break }
    if ([string]::IsNullOrEmpty($np)) { break }
    $curPage = $np
    $curOffset = [int]$no
}
