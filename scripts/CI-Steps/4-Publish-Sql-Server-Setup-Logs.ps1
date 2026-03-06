Write-Line -TextCyan "ENV:SQL = [$($ENV:SQL)]"
$key="$($ENV:SQL)".Split(":")[0]
$target_logs_folder="C:\App\SQL Setup Logs\$key"
Write-Line -TextCyan "target_logs_folder = [$target_logs_folder]"
Publish-SQLServer-SetupLogs $target_logs_folder

