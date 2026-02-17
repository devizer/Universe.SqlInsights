. "$PSScriptRoot\Functions.ps1"
cd Goods\jam.tests
Set-Var "SENSORSAPP_STRESS_WORKINGSET_ROWS" "150000"
Set-Var "SENSORSAPP_STRESS_DURATION" "2000"
BroadCast-Variables
dotnet test Universe.SqlServerJam.Tests.dll
Show-Last-Exit-Code "TEST Universe.SqlServerJam.Tests.dll"

