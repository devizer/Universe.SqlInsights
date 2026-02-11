param(
    [Parameter(Mandatory=$true)]
    [string]$file
)


function show-mem([string]$title) {
   $memDescription = Get-Memory-Info | ForEach-Object { $_.Description }
   Say "[$title $file] Memory: $memDescription. CPU: $(Get-Cpu-Name -includeCoreCount)"
}

$kind=If ("$($ENV:SQL_IMAGE_TAG)" -eq "") { "HOST" } Else { "Container" }
show-mem "Starting on $kind"

# if ("$($ENV:SQL_IMAGE_TAG)" -eq "" -and -not (Test-Path C:\App)) {
if (-not (Test-Path C:\App)) {
  Say "CREATING SYMLINK from '$(Get-Location)' to 'C:\App'"
  cmd /c mklink /d "C:\App" "$(Get-Location)"
}


$relative_file = "scripts\CI-Steps\$file"
if ("$($ENV:SQL_IMAGE_TAG)" -eq "") {
  Say "Invoking locally [$relative_file]"
  Write-Host "Current Directory: $(Get-Location)"
  pwsh -c "`$ErrorActionPreference='Stop'; . `"$relative_file`""
}
Else
{
   Say "Invoking in container [$relative_file]"
   Write-Host "Current Directory: $(Get-Location)"
   # & docker exec sql-server powershell -f "$relative_file"
   & docker exec sql-server pwsh -c "`$ErrorActionPreference='Stop'; . `"$relative_file`""
}

show-mem "Finished on $kind"

# Select-WMI-Objects Win32_Process | Select-Object ProcessId, Name, @{Name="WS(MB)"; Expression={[math]::Round($_.WorkingSetSize / 1MB, 1)}}, CommandLine | ft -AutoSize | Out-String -width 200
# Select-WMI-Objects Win32_Process | Select-Object ProcessId, Name, @{Name="WS(MB)"; Expression={[math]::Round($_.WorkingSetSize / 1MB, 1)}}, CommandLine | Sort-Object ProcessId | ft -AutoSize | Out-String -width 200

