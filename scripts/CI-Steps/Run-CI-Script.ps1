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
if (-not (Test-Path C:\App) -and (Get-OS-Platform) -eq "Windows") {
  Say "CREATING SYMLINK from '$(Get-Location)' to 'C:\App'"
  cmd /c mklink /d "C:\App" "$(Get-Location)"
}


$is_container = [bool]("$($ENV:SQL_IMAGE_TAG)" -ne "")
$title_at = If ($is_container) { "CONTAINER" } Else { "HOST" }
$script_pre="Write-Line -TextMagenta ('$title_at '+(Get-Memory-Info).Description); `$ErrorActionPreference='Stop';"
$script_post = ('if ($Global:LASTEXITCODE) { Write-Line -TextRed "ERROR! STEP ' + $file + ' failed. Exit Code $($Global:LASTEXITCODE)"; exit 1; }')

$relative_file = "scripts\CI-Steps\$file"
if (-not $is_container -or (Get-OS-Platform) -eq "Linux") {
  $ps=if ((Get-OS-Platform) -eq "Windows") { "powershell"} Else { "pwsh" }
  Say "Invoking locally [$relative_file] using '$ps'"
  Write-Host "Current HOST Directory: $(Get-Location)"
  & "$ps" -c "$script_pre; . `"$relative_file`"; $script_post"
}
Else
{
   Say "Invoking in container [$relative_file]"
   Write-Host "Current HOST Directory: $(Get-Location)"
   # & docker exec sql-server powershell -f "$relative_file"
   & docker exec sql-server powershell -c "$script_pre; . `"$relative_file`"; $script_post"
}
$exitCode = $LASTEXITCODE
show-mem "Finished on $kind, Exit Code: $exitCode"


# Select-WMI-Objects Win32_Process | Select-Object ProcessId, Name, @{Name="WS(MB)"; Expression={[math]::Round($_.WorkingSetSize / 1MB, 1)}}, CommandLine | ft -AutoSize | Out-String -width 200
# Select-WMI-Objects Win32_Process | Select-Object ProcessId, Name, @{Name="WS(MB)"; Expression={[math]::Round($_.WorkingSetSize / 1MB, 1)}}, CommandLine | Sort-Object ProcessId | ft -AutoSize | Out-String -width 200

$true | out-null
# $Global:LASTEXITCODE=0
if ($exitCode -ne 0) { throw "STEP $file failed. Exit Code $exitCode" }

