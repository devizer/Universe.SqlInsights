@echo off
for %%d in (Microsoft.Data.SqlClient System.Data.SqlClient) DO (
  sqlcmd -S . -E -Q "Drop Database [SqlInsights Storage %%d Tests]"
)
dotnet build -c Release -f net6.0 -v:q
dotnet test --no-build --nologo -c Release -f net6.0 --filter "Name~Test0_Migrate"