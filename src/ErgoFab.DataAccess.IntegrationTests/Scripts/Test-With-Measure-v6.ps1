cd ..
Import-DevOps
$srcFolder="S:\Ergo-Fab-Tests"
Say "Copy Source to $srcFolder"
New-Item -Path "$srcFolder" -ItemType Directory -Force -EA SilentlyContinue | Out-Null
robocopy ".." $srcFolder /E /XD .vs bin obj node_modules /NFL /NDL
cd $srcFolder\ErgoFab.DataAccess.IntegrationTests


function Find-Test-Cases ([string]$folder) {
    $result = @()
    $files = Get-ChildItem -Path "$folder\*" -Include "*.Internal.Log" | Select-Object -ExpandProperty FullName
    foreach ($file in $files) {
        $lines = Get-Content -Path $file
        foreach ($line in $lines) {
            if ($line.IndexOf("[DbTestPipeline.OnStart] FullFill DB for ") -ge 0) {
                $result += $line
            }
        }
        copy-Item $file $Internal_Log_File
    }
    
    return $result
}

Function Get-Disk-Snapshot() {
    Get-CimInstance -ClassName Win32_PerfRawData_PerfDisk_PhysicalDisk |
        Where-Object { $_.Name -ne "_Total" }
}

$testCasesCache = @(
  @{ IgnoreCache = "True";  Log="NO-cache"; }
  @{ IgnoreCache = "False"; Log="Cached"; }
)

$testCasesDisk = @(
  # @{ Name = "SATA HDD"; Drive="P:"; }
  @{ Name = "NVME SSD"; Drive="C:"; }
)

$testCasesCount = @(
  @{ Name = "8"; Count="0"; }
  @{ Name = "56"; Count="12"; }
)



dotnet build -f net8.0 -v:q > build.log.tmp

$summaryLog="SUMMARY $([DateTime]::Now.ToString("yyyy-MM-dd HH-mm-ss")).LOG"

$ENV:NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES="False"
$ENV:ERGOFAB_TESTS_MUTE_EF_LOGS="True"

foreach($testCaseCount in $testCasesCount) {
foreach($testCaseDisk in $testCasesDisk) {
foreach($testCaseCache in $testCasesCache) {
  $logFile="REPORT $($testCaseCount.Name) tests on $($testCaseDisk.Name) with $($testCaseCache.Log).LOG"
  $Internal_Log_File="REPORT $($testCaseCount.Name) tests on $($testCaseDisk.Name) with $($testCaseCache.Log).Internal.LOG"
  $ENV:ERGOFAB_TESTS_IGNORE_CACHE=$testCaseCache.IgnoreCache
  $ENV:ERGOFAB_TESTS_ADDITIONAL_COUNT=$testCaseCount.Count
  $header="TESTING $($testCaseCount.Name) tests on $($testCaseDisk.Name) with [$($testCaseCache.Log)]"
  Say "$header"
  echo $header >> $summaryLog
  echo (new-object String([char]"-", $header.Length)) >> $summaryLog
  $ENV:ERGOFAB_TESTS_DATA_FOLDER = "$($testCaseDisk.Drive)\Temp\Ergo Fab DB Data"
  
  $before = Get-Disk-Snapshot
  $startTestAt = [System.Diagnostics.Stopwatch]::StartNew()
  dotnet test --no-build -f net8.0 > "$logFile"
  $statusSuccess = If ($?) { "Success" } Else { "Fail" }
  $elapsedSec = $startTestAt.Elapsed.TotalSeconds
  $after = Get-Disk-Snapshot

  echo "Status: $statusSuccess" | tee-object "$logFile" -append | tee-object "$summaryLog" -append

  $tests = Find-Test-Cases bin\Debug\net8.0\TestsOutput
  "" | tee-object "$logFile" -append | tee-object "$summaryLog" -append
  $__ = $tests | % { $_ | tee-object "$logFile" -append }
  "Total Test Count: $($tests.Length)" | tee-object "$logFile" -append | tee-object "$summaryLog" -append
  
  "Elapsed: $([math]::Round($elapsedSec, 1)) sec" | Tee-Object "$logFile" -Append | tee-object "$summaryLog" -append
  $before | ForEach-Object {
      $b = $_
      $a = $after | Where-Object { $_.Name -eq $b.Name }
      $idleSec = [math]::Round(($a.PercentIdleTime - $b.PercentIdleTime) / $b.Frequency_PerfTime, 1)
      [PSCustomObject]@{
          Drive      = $b.Name
          ReadMB     = [math]::Round(($a.DiskReadBytesPersec  - $b.DiskReadBytesPersec)  / 1MB, 2)
          WrittenMB  = [math]::Round(($a.DiskWriteBytesPersec - $b.DiskWriteBytesPersec) / 1MB, 2)
          ReadSec    = [math]::Round(($a.PercentDiskReadTime  - $b.PercentDiskReadTime)  / $b.Frequency_PerfTime, 1)
          WriteSec   = [math]::Round(($a.PercentDiskWriteTime - $b.PercentDiskWriteTime) / $b.Frequency_PerfTime, 1)
          TotalIOSec = [math]::Round(($a.PercentDiskTime      - $b.PercentDiskTime)      / $b.Frequency_PerfTime, 1)
          IdleSec    = $idleSec
          BusySec    = [math]::Round($elapsedSec - $idleSec, 1)
      }
  } | ft -AutoSize | Tee-Object "$logFile" -Append | tee-object "$summaryLog" -append
}}}

Say "Matrix Complete"