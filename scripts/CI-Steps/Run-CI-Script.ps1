param(
    [Parameter(Mandatory=$true)]
    [string]$file
)

if ("$($ENV:SQL_IMAGE_TAG)" -eq "" -and -not (Test-Path C:\App)) {
  Say "CREATING SYMLINK from '$(Get-Location)' to 'C:\App'"
  cmd /c mklink /d "C:\App" "$(Get-Location)"
}


$relative_file = "scripts\CI-Steps\$file"
if ("$($ENV:SQL_IMAGE_TAG)" -eq "") {
  Say "Invoking locally [$relative_file]"
  Write-Host "Current Directory: $(Get-Location)"
  powershell -f "$relative_file"
}
Else
{
   Say "Invoking in container [$relative_file]"
   Write-Host "Current Directory: $(Get-Location)"
   & docker exec sql-server powershell -f "$relative_file"
}
