iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/devizer/Universe.SqlServerJam/master/SqlServer-Version-Management/Install-SqlServer-Version-Management.ps1'))

Find-Local-SqlServers | 
   % { $_.Service } | 
   % { Get-Service -Name $_ } | 
   ? { $_.Status -ne "Running" } |
   % { Write-Host "Starting $($_.Name)"; Start-Service "$($_.Name)" }

$instances1 = @(Find-Local-SqlServers | Select -Property Instance, Version | Populate-Local-SqlServer-Version -Timeout 30)
$instances2 = @(Find-LocalDb-SqlServers | Populate-Local-SqlServer-Version -Timeout 30)
$instances = $instances1 + $instances2

$instances | ft -AutoSize | Out-String -Width 1234 | Out-Host

Create-Directory "Bin"
$instances | ConvertTo-Json -Depth 32 > Bin\instances-arguments.json

$startAt = [System.Diagnostics.Stopwatch]::StartNew()
$index = 0;
$totalErrors=0;
foreach($instance in $instances) {
  $testDuration = [System.Diagnostics.Stopwatch]::StartNew()
  $index++;
  $ENV:SQLINSIGHTS_CONNECTION_STRING="Server=$($instance.Instance); Integrated Security=SSPI; Encrypt=False;Pooling=True"
  $folder = Combine-Path "$pwd" "Bin" "$($instance.Instance.Replace("\",[char]8594))"
  Create-Directory $folder
  echo $instance.Version > (Combine-Path $folder "version.txt")
  $instanceTitle = "[$index of $($instances.Count)] '$($instance.Instance)': $($instance.Version)"
  if ($index -eq 1) { $host.ui.RawUI.WindowTitle = "$instanceTitle" }
  Write-Host $instanceTitle -ForeGroundColor Magenta

  $ENV:SQLINSIGHTS_DATA_DIR = "W:\Temp\Sql Insight Tests\$($instance.Instance.Replace("\","-"))"
  $ENV:SYSTEM_ARTIFACTSDIRECTORY = $folder
  
  pushd ..
  &{ dotnet @("build", "-m:1", "-c", "Release", "-f", "net6.0", "Universe.SqlInsights.SqlServerStorage.Tests.csproj") } *| tee "$folder\build.txt"
  &{ dotnet @("test", "--no-build", "-c", "Release", "-f", "net6.0", "Universe.SqlInsights.SqlServerStorage.Tests.csproj") } *| tee "$folder\test.txt"
  $exitCode = $GLOBAL:LASTEXITCODE
  echo $exitCode > "$folder\exitcode.txt"
  popd
  
  $eta = ($startAt.Elapsed.TotalSeconds / $index * $instances.Count) - $startAt.Elapsed.TotalSeconds;
  Write-Host "DONE: $($instanceTitle)" -ForeGroundColor Green
  Write-Host "ETA: $("{0:n1}" -f $eta)s" -ForeGroundColor Green
  $host.ui.RawUI.WindowTitle = "ETA: $("{0:n1}" -f $eta)s. $instanceTitle"
  $isOk = [bool] ($exitCode -eq 0)
  if (-not $isOk) { $totalErrors++ }
  Set-Property-Smarty $instance "IsOK" $isOk
  Set-Property-Smarty $instance "Duration" ([Math]::Round($testDuration.Elapsed.TotalSeconds, 1))
  $instances | ConvertTo-Json -Depth 32 > Bin\instances-results.json
}

Write-Host "TOTAL TIME: $($startAt.Elapsed)"
Write-Host "TOTAL ERRORS: $($totalErrors)"
