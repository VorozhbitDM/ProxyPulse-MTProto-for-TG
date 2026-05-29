$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0'
$s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$login = Invoke-WebRequest -Uri 'https://tgstat.com/login' -UseBasicParsing -TimeoutSec 30 -UserAgent $ua -WebSession $s
$token = [regex]::Match($login.Content, 'data-telegram-auth-button="([^"]+)"').Groups[1].Value
Write-Host "token=$token"

$body = "auth_key=$([uri]::EscapeDataString($token))"
$r = Invoke-WebRequest -Uri 'https://tgstat.com/auth' -Method POST -Body $body -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing -TimeoutSec 15 -UserAgent $ua -WebSession $s -Headers @{
    Referer = 'https://tgstat.com/login'
    Origin = 'https://tgstat.com'
    'X-Requested-With' = 'XMLHttpRequest'
    Accept = 'application/json, text/javascript, */*; q=0.01'
}
Write-Host "POST auth_key -> $($r.Content)"

$tg = "tg://resolve?domain=tg_analytics_bot&start=$token"
Write-Host "deep link: $tg"
