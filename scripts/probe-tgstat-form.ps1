$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0'
$s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$r1 = Invoke-WebRequest -Uri 'https://tgstat.com/channel/@ProxyMTProto' -UseBasicParsing -TimeoutSec 30 -UserAgent $ua -WebSession $s
$html = $r1.Content

# dump lm-form area
$m = [regex]::Match($html, '<form[^>]*lm-form[\s\S]{0,2500}')
if ($m.Success) { $m.Value | Out-File "$PSScriptRoot\tgstat-form-full.txt" -Encoding utf8; Write-Host "form saved" }

# try meta csrf vs input csrf
$metaCsrf = [regex]::Match($html, 'name="csrf-token"\s+content="([^"]+)"').Groups[1].Value
$inputCsrf = [regex]::Match($html, 'name="_tgstat_csrk"\s+value="([^"]+)"').Groups[1].Value
Write-Host "meta csrf len $($metaCsrf.Length) input csrf len $($inputCsrf.Length) same=$($metaCsrf -eq $inputCsrf)"

$page = [regex]::Match($html, 'class="lm-page"[^>]*value="([^"]+)"').Groups[1].Value
$offset = [regex]::Match($html, 'class="lm-offset"[^>]*value="([^"]+)"').Groups[1].Value

# POST with X-Requested-With
$body = "_tgstat_csrk=$([uri]::EscapeDataString($inputCsrf))&page=$page&offset=$offset"
$r2 = Invoke-WebRequest -Uri 'https://tgstat.com/channel/@ProxyMTProto/posts-last' -Method POST -Body $body -ContentType 'application/x-www-form-urlencoded; charset=UTF-8' -UseBasicParsing -TimeoutSec 30 -UserAgent $ua -WebSession $s -Headers @{
    Referer = 'https://tgstat.com/channel/@ProxyMTProto'
    'X-Requested-With' = 'XMLHttpRequest'
    Accept = '*/*'
}
Write-Host "ajax Server: $(([regex]::Matches($r2.Content, 'Server:')).Count) title $([regex]::Match($r2.Content, '<title>([^<]*)</title>').Groups[1].Value)"

# try tgstat.ru
$r3 = Invoke-WebRequest -Uri 'https://tgstat.ru/channel/@ProxyMTProto' -UseBasicParsing -TimeoutSec 30 -UserAgent $ua -WebSession $s
$htmlRu = $r3.Content
Write-Host "ru page1 Server: $(([regex]::Matches($htmlRu, 'Server:')).Count)"
