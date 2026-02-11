. "$PSScriptRoot\Functions.ps1"

Write-Host "Location: $(Get-Location)"

[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12;
if ($PSVersionTable.PSEdition -ne "Core") {
  [System.Net.ServicePointManager]::ServerCertificateValidationCallback={$true};
}

$urlSource = 'https://devizer.github.io/SqlServer-Version-Management/Install-SqlServer-Version-Management.ps1'; 
foreach($attempt in 1..3) { try { iex ((New-Object System.Net.WebClient).DownloadString($urlSource)); Write-Host "Success: Install-SqlServer-Version-Management.ps1"; break; } catch {sleep 0.1;} }

$memDescription = Get-Memory-Info | ForEach-Object { $_.Description }
Say "Memory: $memDescription"
Write-Host "CPU: $(Get-Cpu-Name -includeCoreCount)"

# Get-ChildItem | Format-Table -Autosize
& choco feature enable -n allowGlobalConfirmation
& choco feature disable -n showDownloadProgress

Say "About BASH"
try { & bash --version } catch {}
try { & bash -c 'uname -a; echo $BASH_VERSION; echo Path is below; echo $PATH' } catch {}

Say "Get-PS1-Repo-Downloads-Folder(): $(Get-PS1-Repo-Downloads-Folder)"
