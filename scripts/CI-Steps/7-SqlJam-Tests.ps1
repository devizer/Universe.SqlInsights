. "$PSScriptRoot\Functions.ps1"
cd C:\App\Goods\jam.tests
$ENV:SENSORSAPP_STRESS_WORKINGSET_ROWS="10000"
$ENV:SENSORSAPP_STRESS_DURATION="200"
dotnet test Universe.SqlServerJam.Tests.dll
Say "Success Complete"
