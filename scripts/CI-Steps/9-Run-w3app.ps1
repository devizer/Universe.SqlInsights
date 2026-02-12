. "$PSScriptRoot\Functions.ps1"

cd C:\App\Goods\w3api\wwwroot

Say "index.html as file"
Get-Content "index.html"

Add-Folder-To-User-Path "$($ENV:USERPROFILE)\.dotnet\tools"
$ENV:PATH="$($ENV:USERPROFILE)\.dotnet\tools;$($ENV:PATH)"

Say "About dotnet serve"
dotnet serve --version 2>$null

Say "LAUNCHING w3app on port 6060 (folder is '$(Get-Location)')"
# cmd /c "start dotnet serve -p 6060"
# Start-Process "dotnet" -ArgumentList "serve -p 6060".Split(" ") -NoNewWindow
# & { dotnet Universe.SqlInsights.W3Api.dll } &
# & { dotnet serve -p 6060 } &
cd C:\App\Goods\w3api\wwwroot
Smart-Start-Process "dotnet" "serve -p 6060"
sleep 2

Open-Url-By-Chrome-On-Windows "http://127.0.0.1:6060"

Say "PROCESSES: dotnet-serve"
Get-Process -Name "dotnet-serve" -EA SilentlyContinue | format-table -autosize | out-host

echo "Waiting 9 seconds ........"
Sleep 9
Show-Chrome-Processes


Say "Validate http connection to http://localhost:6060"
& curl.exe -I http://localhost:6060
Say "curl http://localhost:6060"
& curl.exe http://localhost:6060

Say "Frontend launch completed"
