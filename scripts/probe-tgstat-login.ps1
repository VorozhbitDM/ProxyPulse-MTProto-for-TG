$ua = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0'
$s = New-Object Microsoft.PowerShell.Commands.WebRequestSession
try {
    $login = Invoke-WebRequest -Uri 'https://tgstat.com/login' -UseBasicParsing -TimeoutSec 30 -UserAgent $ua -WebSession $s
    Write-Host "login len $($login.Content.Length)"
    $login.Content | Out-File "$PSScriptRoot\tgstat-login.html" -Encoding utf8
    
    foreach ($pat in @('name="[^"]*login[^"]*"', 'name="[^"]*email[^"]*"', 'name="[^"]*password[^"]*"', 'action="[^"]*login[^"]*"', 'Telegram', 'oauth', 'sign-in')) {
        $m = [regex]::Matches($login.Content, $pat, 'IgnoreCase')
        Write-Host "$pat : $($m.Count)"
        foreach ($x in $m | Select-Object -First 3) { Write-Host "  $($x.Value)" }
    }
} catch {
    Write-Host "ERR $($_.Exception.Message)"
}
