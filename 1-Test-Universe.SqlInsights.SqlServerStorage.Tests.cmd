pushd src\Universe.SqlInsights.SqlServerStorage.Tests
dotnet test --collect:"XPlat Code Coverage" --logger trx
popd