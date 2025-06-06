# LocalDB Uses Same folder for "Sql Insights Warehouse.mdf" 
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/devizer/Universe.SqlServerJam/master/SqlServer-Version-Management/Install-SqlServer-Version-Management.ps1'))

Find-Local-SqlServers | 
   % { $_.Service } | 
   % { Get-Service -Name $_ } | 
   ? { $_.Status -ne "Running" } |
   % { Write-Host "Starting $($_.Name)"; Start-Service "$($_.Name)" }

$instances1 = @(Find-Local-SqlServers | Select -Property Instance, Version | Populate-Local-SqlServer-Version -Timeout 30)
$instances2 = @(Find-LocalDb-SqlServers | Populate-Local-SqlServer-Version -Timeout 30)
$instances = @($instances1 + $instances2 | ? { "$_.Version" -and (-not ("$_.Version" -match "LocalDB"))  -and (-not ("$_.Version" -like "*9.00*")) })

$instances | ft -AutoSize | Out-String -Width 1234 | Out-Host

Create-Directory "Bin"
$instances | ConvertTo-Json -Depth 32 > Bin\instances-arguments.json

$TestBinariesFolder = "$ENV:ERGOFAB_TEST_BINARY_FOLDER"
if (-not (Test-Path $TestBinariesFolder)) {
  Write-Host "Folder Not Found: ERGOFAB_TEST_BINARY_FOLDER = $TestBinariesFolder"
}

if (-not ($ENV:SYSTEM_ARTIFACTSDIRECTORY)) { $ENV:SYSTEM_ARTIFACTSDIRECTORY="$PWD/Bin" }

$thisFolder="$pwd"

$getInstanceTitle = { "$($_.Instance): $($_.Version)" }
$testAction = {
  Write-Host ""; Write-Host "$($_.Instance): $($_.Version)"; 
  $instance = $_

  $safeInstanceName = "$($instance.Instance.Replace("\",[char]8594))"
  $logFile = Combine-Path "$thisFolder" "Bin" "$safeInstanceName"
  
  $ENV:NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES = If (Is-BuildServer) { "True" } Else { "False" }
  pushd "$TestBinariesFolder"
  $dataDrive = if (Test-Path "W:") { "W:" } Else { "T:" }
  $ENV:ERGOFAB_TESTS_DATA_FOLDER="$dataDrive\Temp\ErgFab-Tests-SQL-Data\$safeInstanceName"
  $ENV:ERGOFAB_TESTS_MASTER_CONNECTIONSTRING="Server=$($instance.Instance);Encrypt=False;Integrated Security=SSPI"
  $ENV:ERGOFAB_TESTS_HISTORY_CONNECTIONSTRING="$ENV:ERGOFAB_TESTS_MASTER_CONNECTIONSTRING; Initial Catalog=Sql Insights Warehouse; Pooling=True"
  $ENV:ERGOFAB_TESTS_REPORT_FULLNAME="$ENV:SYSTEM_ARTIFACTSDIRECTORY\\$safeInstanceName.Insights.txt"
  foreach($var in @("ERGOFAB_TESTS_DATA_FOLDER", "ERGOFAB_TESTS_HISTORY_CONNECTIONSTRING", "NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES")) {
    $v = [Environment]::GetEnvironmentVariable($var)
    echo "$var = [$($v)]" 
  }

  & { dotnet test --filter "FullyQualifiedName !~ ExportToNullStream" ErgoFab.DataAccess.IntegrationTests.dll } *| tee "$logFile.Output.txt" | out-host
  # cp -av TestsOutput $SYSTEM_ARTIFACTSDIRECTORY

  popd
}

$errors = @($instances | Try-Action-ForEach -ActionTitle "Test ErgoFAB" -Action $testAction -ItemTitle $getInstanceTitle)
$totalErrors = $errors.Count;

if ($totalErrors -gt 0) { throw "Failed counter = $totalErrors" }
