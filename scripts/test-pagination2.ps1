$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
$entry = 'https://web.archive.org/web/2/https://t.me/s/ProxyMTProto'
$r1 = Invoke-WebRequest -Uri $entry -UseBasicParsing -TimeoutSec 45 -UserAgent $ua
$before = [regex]::Match($r1.Content, 'before=(\d+)').Groups[1].Value
$snap = [regex]::Match($r1.Content, '/web/(\d{14})').Groups[1].Value
Write-Host "before=$before snap=$snap"
Start-Sleep -Seconds 8
$urls = @(
    "https://web.archive.org/web/${snap}im_/https://t.me/s/ProxyMTProto?before=$before",
    "https://web.archive.org/web/${snap}*/https://t.me/s/ProxyMTProto?before=$before",
    "https://web.archive.org/web/${snap}if_/https://t.me/s/ProxyMTProto?before=$before"
)
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
foreach ($c in $r1.Headers['Set-Cookie'] -split ',') { } 
foreach ($u in $urls) {
    Write-Host "`n$u"
    try {
        $r = Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 60 -UserAgent $ua -WebSession $session -Headers @{
            Referer = $entry
            'Accept-Language' = 'en-US,en;q=0.9'
        }
        Write-Host "OK" $r.StatusCode $r.Content.Length
    } catch { Write-Host "FAIL" $_.Exception.Message }
    Start-Sleep 5
}

# CDX newest snapshots as pages
Write-Host "`n=== CDX pages ==="
$cdx = Invoke-WebRequest -Uri 'https://web.archive.org/cdx/search/cdx?url=t.me/s/ProxyMTProto&output=text&fl=timestamp&collapse=timestamp&limit=15' -UseBasicParsing -TimeoutSec 40 -UserAgent $ua
$ts = $cdx.Content -split "`n" | Where-Object { $_ -match '^\d{14}$' }
$last3 = $ts | Select-Object -Last 3
foreach ($t in $last3) {
    $u = "https://web.archive.org/web/${t}if_/https://t.me/s/ProxyMTProto"
    Write-Host $u
    try {
        $r = Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 45 -UserAgent $ua
        Write-Host "OK proxy links:" ([regex]::Matches($r.Content,'/proxy\?').Count)
    } catch { Write-Host "FAIL" $_.Exception.Message }
    Start-Sleep 3
}
