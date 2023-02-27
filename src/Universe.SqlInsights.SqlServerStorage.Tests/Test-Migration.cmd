@echo off
for %%d in (Microsoft.Data.SqlClient System.Data.SqlClient) DO (
  sqlcmd -S . -E -Q "Drop Database [SqlInsights Storage %%d Tests]"
)
dotnet build -c Reelase -f netcoreapp3.1 -v:q
dotnet test --no-build --nologo -c Reelase -f netcoreapp3.1 --filter "Name~Test0_Migrate"