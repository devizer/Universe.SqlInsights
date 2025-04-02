prj=ErgoFab.DataAccess.IntegrationTests.csproj
ver="3.14.0.1119"
sed -i '/Universe.NUnitPipeline.SqlServerDatabaseFactory.csproj/d' "$prj"
sed -i '/Universe.SqlInsights.NUnit.csproj/d' "$prj"
dotnet add "$prj" package Universe.NUnitPipeline.SqlServerDatabaseFactory -v "$ver"
dotnet add "$prj" package Universe.SqlInsights.NUnit -v "$ver"
dotnet test -c Release -f net8.0