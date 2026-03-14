Function Get-Disk-Snapshot() {
    Get-CimInstance -ClassName Win32_PerfRawData_PerfDisk_PhysicalDisk |
        Where-Object { $_.Name -ne "_Total" }
}

$before = Get-Disk-Snapshot

$ENV:ERGOFAB_TESTS_DATA_FOLDER = "P:\Temp\Ergo Fab DB Data"

$startTestAt = [System.Diagnostics.Stopwatch]::StartNew()
dotnet test -f net8.0 > test.log.tmp
"Elapsed: $([math]::Round($startTestAt.Elapsed.TotalSeconds, 1)) sec" | Tee-Object test.log.tmp -Append

$after = Get-Disk-Snapshot

$before | ForEach-Object {
    $b = $_
    $a = $after | Where-Object { $_.Name -eq $b.Name }
    [PSCustomObject]@{
        Drive      = $b.Name
        ReadMB     = [math]::Round(($a.DiskReadBytesPersec  - $b.DiskReadBytesPersec)  / 1MB, 2)
        WrittenMB  = [math]::Round(($a.DiskWriteBytesPersec - $b.DiskWriteBytesPersec) / 1MB, 2)
        ReadSec    = [math]::Round(($a.PercentDiskReadTime  - $b.PercentDiskReadTime)  / $b.Frequency_PerfTime, 1)
        WriteSec   = [math]::Round(($a.PercentDiskWriteTime - $b.PercentDiskWriteTime) / $b.Frequency_PerfTime, 1)
        TotalIOSec = [math]::Round(($a.PercentDiskTime      - $b.PercentDiskTime)      / $b.Frequency_PerfTime, 1)
        IdleSec    = [math]::Round(($a.PercentIdleTime      - $b.PercentIdleTime)      / $b.Frequency_PerfTime, 1)
    }
} | ft -AutoSize | Tee-Object test.log.tmp -Append
