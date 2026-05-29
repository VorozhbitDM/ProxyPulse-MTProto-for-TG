$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0'
$s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$html = (Invoke-WebRequest -Uri 'https://tgstat.com/channel/@ProxyMTProto' -UseBasicParsing -TimeoutSec 30 -UserAgent $ua -WebSession $s).Content
$csrf = [regex]::Match($html, 'name="_tgstat_csrk"\s+value="([^"]+)"').Groups[1].Value
$page = [regex]::Match($html, 'class="lm-page"[^>]*value="([^"]+)"').Groups[1].Value
$offset = [regex]::Match($html, 'class="lm-offset"[^>]*value="([^"]+)"').Groups[1].Value
Write-Host "start page=$page offset=$offset"

function Count-Proxies($text) {
    $keys = New-Object System.Collections.Generic.HashSet[string]
    foreach ($m in [regex]::Matches($text, 'Server:\s*(?:<[^>]+>)*\s*(.+?)\s*(?:<[^>]+>)*\s*Port:\s*(?:<[^>]+>)*\s*(\d+)', 'IgnoreCase')) {
        $s = ($m.Groups[1].Value -replace '<[^>]+>','').Trim()
        $keys.Add("$s`:$($m.Groups[2].Value)") | Out-Null
    }
    return $keys.Count
}

$curPage = $page
$curOffset = [int]$offset
$total = Count-Proxies $html
Write-Host "page0 proxies $total"

for ($n = 1; $n -le 5; $n++) {
    $body = "_tgstat_csrk=$([uri]::EscapeDataString($csrf))&page=$curPage&offset=$curOffset&hideDeleted=1"
    $r = Invoke-WebRequest -Uri 'https://tgstat.com/channel/@ProxyMTProto/posts-last' -Method POST -Body $body -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -TimeoutSec 30 -UserAgent $ua -WebSession $s -Headers @{
        Referer = 'https://tgstat.com/channel/@ProxyMTProto'
        Origin = 'https://tgstat.com'
        'X-Requested-With' = 'XMLHttpRequest'
    }
    $json = $r.Content
    if ($json -match '"status":"restricted"') { Write-Host "page$n RESTRICTED"; break }
    $htmlChunk = $json
    if ($json.TrimStart().StartsWith('{')) {
        $m = [regex]::Match($json, '"html":"')
        Write-Host "page$n json len $($json.Length) status $(if($json -match '""status"":""([^""]+)""'){$matches[1]}else{'?'})"
        # extract offset from json root
        $om = [regex]::Match($json, '"offset"\s*:\s*"?(\d+)"?')
        $pm = [regex]::Match($json, '"page"\s*:\s*"?([^",}]+)"?')
        if ($om.Success) { Write-Host "  json offset $($om.Groups[1].Value)" }
        if ($pm.Success) { Write-Host "  json page $($pm.Groups[1].Value)" }
    }
    $chunkProxies = Count-Proxies $json
    Write-Host "  chunk proxy refs $chunkProxies"
    $np = [regex]::Match($json, 'class=""lm-page""[^>]*value=""([^""]+)""').Groups[1].Value
    $no = [regex]::Match($json, 'class=""lm-offset""[^>]*value=""([^""]+)""').Groups[1].Value
    if ($np) { $curPage = $np; Write-Host "  html page $np" }
    if ($no) { $curOffset = [int]$no; Write-Host "  html offset $no" } else { $curOffset += 20; Write-Host "  offset += 20 -> $curOffset" }
    if ($chunkProxies -eq 0 -and $json -notmatch 'Server:') { break }
    Start-Sleep -Milliseconds 900
}
