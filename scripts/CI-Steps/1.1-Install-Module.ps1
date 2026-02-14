. "$PSScriptRoot\Functions.ps1"

Write-Host "Location: $(Get-Location)"
echo "`$PSNativeCommandArgumentPassing = [$PSNativeCommandArgumentPassing]"
& choco feature enable -n allowGlobalConfirmation
& choco feature disable -n showDownloadProgress


[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12;
if ($PSVersionTable.PSEdition -ne "Core") {
  [System.Net.ServicePointManager]::ServerCertificateValidationCallback={$true};
}

$urlSource = 'https://devizer.github.io/SqlServer-Version-Management/Install-SqlServer-Version-Management.ps1'; 
foreach($attempt in 1..3) { try { iex ((New-Object System.Net.WebClient).DownloadString($urlSource)); Write-Host "Success: Install-SqlServer-Version-Management.ps1"; break; } catch {sleep 0.1;} }

Say "Get-PS1-Repo-Downloads-Folder(): $(Get-PS1-Repo-Downloads-Folder)"



Write-Artifact-Info "OS-NAME.TXT" "$(Get-OS-Name)"
Write-Artifact-Info "CPU-NAME.TXT" "$(Get-Cpu-Name -includeCoreCount)"
Write-Artifact-Info "MEMORY-INFO.TXT" "$((Get-Memory-Info).Description)"
# TODO: Write SQL Version to SQL-VERSION.TXT



