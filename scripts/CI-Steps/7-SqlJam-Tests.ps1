. "$PSScriptRoot\Functions.ps1"
cd Goods\jam.tests
BroadCast-Variables
dotnet test Universe.SqlServerJam.Tests.dll
Show-Last-Exit-Code "TEST Universe.SqlServerJam.Tests.dll" -Throw

