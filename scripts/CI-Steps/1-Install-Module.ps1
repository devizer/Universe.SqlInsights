Write-Host "Location: $(Get-Location)"
$urlSource = 'https://devizer.github.io/SqlServer-Version-Management/SqlServer-Version-Management.ps1'; 
foreach($attempt in 1..3) { try { iex ((New-Object System.Net.WebClient).DownloadString($urlSource)); break; } catch {sleep 0.1;} }

Get-ChildItem | Format-Table -Autosize
