pushd src\Universe.SqlInsights.SqlServerStorage.Tests
rem dotnet test --collect:"XPlat Code Coverage" --logger trx
dotnet test -c Release -f net6.0
popd