cd C:\App\Goods\w3api\wwwroot
Say "CONTENT FOR $(Get-Location)"
Get-ChildItem | format-table

Say "Installing dotnet serve"
dotnet tool install --global dotnet-serve

Say "About dotnet serve"
dotnet serve --version 2>$null

Say "LAUNCHING"
cmd /c "start dotnet serve -p 6060"
sleep 1

Say "PROCESSES"
Get-Process -Name "dotnet-serve" | format-table -autosize | out-host


Say "Validate http connection to http://localhost:6060"
& curl.exe -I http://localhost:6060

$true