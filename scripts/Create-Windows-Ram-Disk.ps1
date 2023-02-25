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

Get-NetIPAddress | % {$_.IpAddress} | where { $_.StartsWith("10.") } | % { if ($_) {$ip=$_} }
Get-NetIPAddress | % {$_.IpAddress} | where { $_.StartsWith("172.") } | % { if ($_) {$ip=$_} }

# works only
Get-NetIPAddress | % {$_.IpAddress} | where { $_.StartsWith("192.") } | % { if ($_) {$ip=$_} }
Say "DETECTED IP: [$ip]"

$size=3000;
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
New-iscsivirtualdisk -path ramdisk:RAMDISK1.vhdx -size ([int]$size * 1MB)
New-IscsiServerTarget Target1 -InitiatorId IPAddress:$ip
Add-IscsiVirtualDiskTargetMapping -TargetName Target1 -Path ramdisk:RAMDISK1.vhdx -Lun 1
Start-Service msiscsi
New-IscsiTargetPortal -TargetPortalAddress $ip
Get-IscsiTarget | Connect-IscsiTarget
Get-IscsiConnection | Get-Disk | Set-Disk -IsOffline $False
Get-IscsiConnection | Get-Disk | Initialize-Disk -PartitionStyle MBR
# -AssignDriveLetter  random?
Get-IscsiConnection | Get-Disk | New-Partition -UseMaximumSize -DriveLetter ([char]"$($ENV:RAM_DISK)") | Format-Volume -FileSystem NTFS -NewFileSystemLabel RamDisk -Full -Force

echo ""
Say "GET-DISK"
get-disk | ft
Say "Get-PSDrive"
Get-PSDrive | ft

$ram=Get-Ram;
Say "Total RAM: $($ram.Total.ToString("n0")) MB. Free: $($ram.Free.ToString("n0")) MB ($([Math]::Round($ram.Free * 100 / $ram.Total, 1))%)"