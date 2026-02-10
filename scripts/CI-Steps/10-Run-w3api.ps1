function Smart-Start-Process([string] $exe, [string] $parameters) {
   $psi = New-Object System.Diagnostics.ProcessStartInfo
   $psi.FileName = $exe
   $psi.Arguments = $parameters
   $psi.UseShellExecute = $true
   $psi.CreateNoWindow = $true
   # $psi.RedirectStandardOutput = $false 
   # $psi.RedirectStandardError = $false  
   $proc = [System.Diagnostics.Process]::Start($psi)
}

cd C:\App\Goods\w3api
$ENV:ASPNETCORE_URLS="http://*:50420"
# Start-Process "dotnet" -ArgumentList "Universe.SqlInsights.W3Api.dll".Split(" ") -NoNewWindow
# & { dotnet Universe.SqlInsights.W3Api.dll } &
Smart-Start-Process "dotnet" "Universe.SqlInsights.W3Api.dll"

Sleep 60
