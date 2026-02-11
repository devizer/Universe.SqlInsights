. "$PSScriptRoot\Functions.ps1"

show-mem

cd C:\App\Goods\w3api\wwwroot
Say "CONTENT FOR $(Get-Location)"
Get-ChildItem | format-table

Say "index.html as file"
Get-Content "index.html"

Say "Installing dotnet serve"
dotnet tool install --global dotnet-serve

Say "About dotnet serve"
dotnet serve --version 2>$null

Say "LAUNCHING w3app on port 6060 (folder is '$(Get-Location)')"
# cmd /c "start dotnet serve -p 6060"
# Start-Process "dotnet" -ArgumentList "serve -p 6060".Split(" ") -NoNewWindow
# & { dotnet Universe.SqlInsights.W3Api.dll } &
# & { dotnet serve -p 6060 } &
cd C:\App\Goods\w3api\wwwroot
Smart-Start-Process "dotnet" "serve -p 6060"
sleep 1

Open-Url-By-Chrome-On-Windows "http://127.0.0.1:6060"

Say "PROCESSES: dotnet-serve"
Get-Process -Name "dotnet-serve" | format-table -autosize | out-host

echo "Waiting 9 seconds ........"
Sleep 9
Say "PROCESSES: chrome"
Show-Chrome


Say "Validate http connection to http://localhost:6060"
& curl.exe -I http://localhost:6060
Say "curl http://localhost:6060"
& curl.exe http://localhost:6060

show-mem

Say "9-Run-w3app.ps1 completed"
