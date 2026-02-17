. "$PSScriptRoot\Functions.ps1"

cd Goods\w3api\wwwroot

Show-Chrome-Program-List

Say "index.html as file"
Get-Content "index.html"

# Either host or container
dotnet tool install -g dotnet-serve

if ((Get-Os-Platform) -eq "Windows") {
  Add-Folder-To-User-Path "$($ENV:USERPROFILE)\.dotnet\tools"
  $ENV:PATH="$($ENV:USERPROFILE)\.dotnet\tools;$($ENV:PATH)"
}
Else {
  $ENV:PATH="$($ENV:USERPROFILE)/.dotnet/tools:$($ENV:PATH)"
}

Say "About dotnet serve"
& dotnet serve --version 2>$null

Say "LAUNCHING w3app on port 6060 (folder is '$(Get-Location)')"
# cmd /c "start dotnet serve -p 6060"
# Start-Process "dotnet" -ArgumentList "serve -p 6060".Split(" ") -NoNewWindow
# & { dotnet Universe.SqlInsights.W3Api.dll } &
# & { dotnet serve -p 6060 } &
# ALREADY AT cd C:\App\Goods\w3api\wwwroot

Smart-Start-Process "dotnet" "serve -p 6060"
Start-Sleep -Seconds 2

Open-Url-By-Chrome-On-Windows "http://127.0.0.1:6060"

Say "PROCESSES: dotnet-serve"
Get-Process -Name "dotnet-serve" -EA SilentlyContinue | format-table -autosize | out-host

echo "Waiting 9 seconds ........"
Start-Sleep -Seconds 9
Show-Chrome-Processes


# --no-progress-meter was introduced in version 7.67, 2019
Say "Validate http connection to http://localhost:6060"
& (Get-Command curl -CommandType Application | Select-Object -First 1).Source --no-progress-meter -I http://localhost:6060
Say "curl http://localhost:6060"
& (Get-Command curl -CommandType Application | Select-Object -First 1).Source --no-progress-meter http://localhost:6060

Say "Frontend launch completed"
