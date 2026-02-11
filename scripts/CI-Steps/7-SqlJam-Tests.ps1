. "$PSScriptRoot\Functions.ps1"
cd C:\App\Goods\jam.tests
Set-Var "SENSORSAPP_STRESS_WORKINGSET_ROWS" "100000"
Set-Var "SENSORSAPP_STRESS_DURATION" "2000"
dotnet test Universe.SqlServerJam.Tests.dll
Say "Success Complete"
