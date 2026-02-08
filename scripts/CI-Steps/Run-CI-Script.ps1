param(
    [Parameter(Mandatory=$true)]
    [string]$file
)

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
