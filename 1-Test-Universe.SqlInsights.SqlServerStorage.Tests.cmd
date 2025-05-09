pushd src\Universe.SqlInsights.SqlServerStorage.Tests
rem dotnet test --collect:"XPlat Code Coverage" --logger trx
dotnet test -c Release -f net6.0
If ErrorLevel 1 Goto :error
popd
goto :finish

:error
echo ERROR!!! Universe.SqlInsights.SqlServerStorage.Tests
echo ERROR!!! Universe.SqlInsights.SqlServerStorage.Tests
echo ERROR!!! Universe.SqlInsights.SqlServerStorage.Tests
echo ERROR!!! Universe.SqlInsights.SqlServerStorage.Tests
exit 77

:finish