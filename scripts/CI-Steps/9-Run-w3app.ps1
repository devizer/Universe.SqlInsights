function show-mem() {
   $memDescription = Get-Memory-Info | ForEach-Object { $_.Description }
   Say "Memory: $memDescription"
   Write-Host "CPU: $(Get-Cpu-Name -includeCoreCount)"
}

show-mem

cd C:\App\Goods\w3api\wwwroot
Say "CONTENT FOR $(Get-Location)"
Get-ChildItem | format-table

Say "index.html"
Get-Content "index.html"

Say "Installing dotnet serve"
dotnet tool install --global dotnet-serve

Say "About dotnet serve"
dotnet serve --version 2>$null

Say "LAUNCHING w3app on port 6060"
# cmd /c "start dotnet serve -p 6060"
Start-Process "dotnet" -ArgumentList "serve -p 6060 -o".Split(" ")
sleep 1

Say "PROCESSES: dotnet-serve"
Get-Process -Name "dotnet-serve" | format-table -autosize | out-host

echo "Waiting ........"
Sleep 9
Say "PROCESSES: chrome"
Get-Process | where-object { "$($_.ProcessName)" -match "chrome" } | format-table -autosize


Say "Validate http connection to http://localhost:6060"
& curl.exe -I http://localhost:6060
Say "curl /"
& curl.exe http://localhost:6060

show-mem

$true
