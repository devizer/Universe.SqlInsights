function Get-FreePort { $listener = [System.Net.Sockets.TcpListener]0; $listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop(); return $port }

function Smart-Start-Process([string] $exe, [string] $parameters) {
   $psi = New-Object System.Diagnostics.ProcessStartInfo
   $psi.FileName = $exe
   $psi.Arguments = $parameters
   $psi.UseShellExecute = $false       
   $psi.CreateNoWindow = $false         
   # $psi.RedirectStandardOutput = $false 
   # $psi.RedirectStandardError = $false  
   $proc = [System.Diagnostics.Process]::Start($psi)
}

function Open-Url-By-Chrome-On-Windows([string] $url) {
   foreach($candidate in @("C:\Program Files (x86)\Chromium\Application\chrome.exe", "C:\Program Files\Chromium\Application\chrome.exe")) {
     if (Test-Path $candidate) { $chromePath=$candidate }
   }
   try { $ver = (Get-Item "$chromePath").VersionInfo.ProductVersion; Write-Host "BROWSER VERSION $ver OPENING $url ..." } catch {}
   if (-not (Test-Path $chromePath)) {
      Write-Host "[Open-Url-By-Chrome] WARNING! Chromium is missing '$chromePath'" -ForeGroundColor Red;
      return $false
   }

   $chromeArgs = @(
       "--headless",
       "--disable-gpu",
       "--enable-logging",
       "--no-first-run",
       "--no-sandbox",
       "--remote-debugging-port=$(Get-FreePort)",
       "$url"
   )

   # Start-Process $chromePath -ArgumentList $chromeArgs
   Smart-Start-Process $chromePath "$chromeArgs"
}

function Show-Chrome() {
  $chomes = @(Get-Process | Where-Object { $_.ProcessName -match "chrome" })

  if (-not $chomes) {
    Write-Host "Browser Chrome is not running" -ForeGroundColor Red;
  } else {
    $megabytes = (Get-Process chrome | Measure-Object WorkingSet64 -Sum).Sum / 1MB
    $megabytes = [Math]::Round($megabytes,1)
    Say "Total $($chomes.Count) processes are running, total $megabytes MB"
    $chomes | Format-Table -autosize | Out-String -width 123 | Out-Host 
  }
}

function Kill-Chrome() {
  & taskkill /f /t /im chrome.exe 2>$null
}

function show-mem() {
   $memDescription = Get-Memory-Info | ForEach-Object { $_.Description }
   Say "Memory: $memDescription"
   Write-Host "CPU: $(Get-Cpu-Name -includeCoreCount)"
}

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
Smart-Start-Process "dotnet" "serve -p 6060"
sleep 1

Open-Url-By-Chrome-On-Windows "http://127.0.0.1:6060"

Say "PROCESSES: dotnet-serve"
Get-Process -Name "dotnet-serve" | format-table -autosize | out-host

echo "Waiting ........"
Sleep 9
Say "PROCESSES: chrome"
Show-Chrome


Say "Validate http connection to http://localhost:6060"
& curl.exe -I http://localhost:6060
Say "curl http://localhost:6060"
& curl.exe http://localhost:6060

show-mem

Say "9-Run-w3app.ps1 completed"
