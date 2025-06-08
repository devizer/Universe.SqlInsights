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

$getInstanceTitle = { "$($_.Instance): $($_.Version)" }
$testAction = {
  Write-Host ""; Write-Host "$($_.Instance): $($_.Version)"; 
  $instance = $_
  $ENV:SQLINSIGHTS_CONNECTION_STRING="Server=$($instance.Instance); Integrated Security=SSPI; Encrypt=False;Pooling=True"
  Write-Host "SQLINSIGHTS_CONNECTION_STRING = [$($ENV:SQLINSIGHTS_CONNECTION_STRING)]"
  $folder = Combine-Path "$pwd" "Bin" "$($instance.Instance.Replace("\",[char]8594))"
  Create-Directory $folder
  echo $instance.Version > (Combine-Path $folder "version.txt")

  $dataDrive = if (Test-Path "W:") { "W:" } Else { "T:" }
  $ENV:SQLINSIGHTS_DATA_DIR = "$dataDrive\Temp\Sql Insight Tests\$($instance.Instance.Replace("\","-"))"
  $ENV:SYSTEM_ARTIFACTSDIRECTORY = $folder
  Remove-Item "$folder\AddAction.log" -Force -EA SilentlyContinue
  
  pushd ..
  &{ dotnet @("build", "-m:1", "-c", "Release", "-f", "net6.0", "Universe.SqlInsights.SqlServerStorage.Tests.csproj") } *| tee "$folder\build.txt" | out-host
  &{ dotnet @("test", "--no-build", "-c", "Release", "-f", "net6.0", "Universe.SqlInsights.SqlServerStorage.Tests.csproj") } *| tee "$folder\test.txt" | out-host
  $exitCode = $GLOBAL:LASTEXITCODE
  echo $exitCode > "$folder\exitcode.txt"
  popd
  
  $instances | ConvertTo-Json -Depth 32 > Bin\instances-results.json
}

$errors = @($instances | Try-Action-ForEach -ActionTitle "Test Storage" -Action $testAction -ItemTitle $getInstanceTitle)
$totalErrors = $errors.Count;

if ($totalErrors -gt 0) { throw "Failed counter = $totalErrors" }
