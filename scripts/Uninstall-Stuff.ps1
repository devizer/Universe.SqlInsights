# msiexec.exe /x "{588A9A11-1E20-4B91-8817-2D36ACBBBF9F}" /q 
function Get-Elapsed
{
    if ($Global:startAt -eq $null) { $Global:startAt = [System.Diagnostics.Stopwatch]::StartNew(); }
    [System.String]::Concat("[", (new-object System.DateTime(0)).AddMilliseconds($Global:startAt.ElapsedMilliseconds).ToString("mm:ss"), "]");
}; Get-Elapsed | out-null;

function Say { param( [string] $message )
    Write-Host "$(Get-Elapsed) " -NoNewline -ForegroundColor Magenta
    Write-Host "$message" -ForegroundColor Yellow
}

# {41C0DB18-1790-465E-B0DD-D9CAA35CACBE} 15.0.1300.359, Microsoft Command Line Utilities 15 for SQL ServerCommand line tools
# {853997DA-6FCB-4FB9-918E-E0FF881FAF65} 17.7.2.1, Microsoft ODBC Driver 17 for SQL Server
# {4D454BAB-A1F7-492D-9300-5BF05DB30BEB} 18.6.3.0, Microsoft OLE DB Driver for SQL Server

$ids=@(
  "{D113B5D0-D291-4FCD-8609-17265878F921}",
  "{A5B947FD-875D-448C-AB7C-A737CA9FEA4C}",
  "{E3806E56-5AF2-4467-A6B8-F78331A9F194}",
  "{5BC7E9EB-13E8-45DB-8A60-F2481FEB4595}",
  "{36E492B8-CB83-4DA5-A5D2-D99A8E8228A1}",
  "{3366C1AB-89B9-4B61-A388-0B2CEC1F6863}",
  "{8C352959-35A5-40CA-A49C-91B349AB2778}"
)

$undir="$($Env:SYSTEM_ARTIFACTSDIRECTORY)\Uninstall.Logs"
New-Item -ItemType Directory -Path "$undir" -EA SilentlyContinue | out-null


for ($i=0; $i -lt $ids.Length; $i++) {
  $id=$ids[$i];
  Say "Uninistalling [$($i+1) of $($ids.Length)]: $id"
  # & msiexec.exe /x "{588A9A11-1E20-4B91-8817-2D36ACBBBF9F}" /q 
  cmd /c msiexec /x "$id" /q /L*v "$undir\$id.log"
  Say "Uninistalled [$($i+1) of $($ids.Length)]"
}
