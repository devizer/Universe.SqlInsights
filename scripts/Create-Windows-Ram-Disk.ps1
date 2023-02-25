function Get-Elapsed
{
    if ($Global:startAt -eq $null) { $Global:startAt = [System.Diagnostics.Stopwatch]::StartNew(); }
    [System.String]::Concat("[", (new-object System.DateTime(0)).AddMilliseconds($Global:startAt.ElapsedMilliseconds).ToString("mm:ss"), "]");
}; Get-Elapsed | out-null;

function Say { param( [string] $message )
    Write-Host "$(Get-Elapsed) " -NoNewline -ForegroundColor Magenta
    Write-Host "$message" -ForegroundColor Yellow
}

function Get-Ram() {
    $mem=(Get-CIMInstance Win32_OperatingSystem | Select FreePhysicalMemory,TotalVisibleMemorySize)[0];
    return @{
        Total=[int] ($mem.TotalVisibleMemorySize / 1024);
        Free=[int] ($mem.FreePhysicalMemory / 1024);
    }
}
function Get-CPU() {
    return "$((Get-WmiObject Win32_Processor).Name), $([System.Environment]::ProcessorCount) Cores";
}

Say "WORKING DIR '$PWD'"
Say "CPU: $(Get-CPU)"
$ram=Get-Ram;
Say "Total RAM: $($ram.Total.ToString("n0")) MB. Free: $($ram.Free.ToString("n0")) MB ($([Math]::Round($ram.Free * 100 / $ram.Total, 1))%)"

# List of IP Addresses
Get-NetIPAddress | ft
$ip=""

Get-NetIPAddress | % {$_.IpAddress} | where { $_.StartsWith("172.") } | % { if ($_) {$ip=$_} }
Get-NetIPAddress | % {$_.IpAddress} | where { $_.StartsWith("10.") } | % { if ($_) {$ip=$_} }

# works only
Get-NetIPAddress | % {$_.IpAddress} | where { $_.StartsWith("192.") } | % { if ($_) {$ip=$_} }
Say "DETECTED IP: [$ip]"

$size=2000; if ("$($ENV:RAM_DISK_SIZE)") { $size=$($ENV:RAM_DISK_SIZE); }

Say "RAM DISK SIZE: $size MB";
Say "RAM DISK DRIVE: '$($ENV:RAM_DISK)'";


$M=Get-Module -ListAvailable ServerManager; Import-Module -ModuleInfo $M;
# Say "WINDOWS Features"; Get-WindowsFeature *; echo "";

@("FS-iSCSITarget-Server", "iSCSITarget-VSS-VDS") | % { $package=$_
  Say "Installing $package"
  Install-WindowsFeature $package;
  Say "Installed $package";
  echo "";
}


# https://github.com/aso930/CreateRAMDISK


cd ~
Say "New-iscsivirtualdisk ..."
New-iscsivirtualdisk -path ramdisk:RAMDISK1.vhdx -size ([int]$size * 1MB)
Say "New-IscsiServerTarget ..."
New-IscsiServerTarget Target1 -InitiatorId IPAddress:$ip
Say "Add-IscsiVirtualDiskTargetMapping ..." # FAIL
Add-IscsiVirtualDiskTargetMapping -TargetName Target1 -Path ramdisk:RAMDISK1.vhdx # -Lun 1
Say "Start-Service msiscsi ..."
Start-Service msiscsi
Say "New-IscsiTargetPortal ..."
New-IscsiTargetPortal -TargetPortalAddress $ip
Say "Connect-IscsiTarget ..."
Get-IscsiTarget | Connect-IscsiTarget
Say "Set-Disk -IsOffline False ..."
Get-IscsiConnection | Get-Disk | Set-Disk -IsOffline $False
Say "Initialize-Disk ..."
Get-IscsiConnection | Get-Disk | Initialize-Disk -PartitionStyle MBR
# -AssignDriveLetter  random?
Say "New-Partition and Format-Volume ..."
Get-IscsiConnection | Get-Disk | New-Partition -UseMaximumSize -DriveLetter ([char]"$($ENV:RAM_DISK)") | Format-Volume -FileSystem NTFS -NewFileSystemLabel RamDisk -Full -Force
Say "COMPLEEEEETE"

echo ""
Say "GET-DISK"
get-disk | ft
Say "Get-PSDrive"
Get-PSDrive | ft

$ram=Get-Ram;
Say "Total RAM: $($ram.Total.ToString("n0")) MB. Free: $($ram.Free.ToString("n0")) MB ($([Math]::Round($ram.Free * 100 / $ram.Total, 1))%)"

$ramDiskLetter="$($ENV:RAM_DISK)";
if (Test-Path -Path "$($ramDiskLetter):\") {
    Say "RAM Drive $($ramDiskLetter):\ exists"
    $dataDir="$($ramDiskLetter):\DB";
    New-Item $dataDir -type directory -force -EA SilentlyContinue | out-null
    echo "##vso[task.setvariable variable=DB_DATA_DIR]$dataDir"
    Say "DB_DATA_DIR env is set to $dataDir";
} else {
    Say "RAM Drive $($ramDiskLetter):\ not found"
}
echo "finished"

# HOW TO REMOVE
# Remove-IscsiVirtualDiskTargetMapping -TargetName Target1 -DevicePath ramdisk:RAMDISK1.vhdx
# Remove-IscsiServerTarget -TargetName Target1
# Remove-IscsiVirtualDisk -Path "ramdisk:tempdbRAM.vhdx" 
