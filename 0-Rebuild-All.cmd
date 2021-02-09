If Not Defined VER Set VER=0.1.2.3
for %%d in (Universe.SqlInsights.AspNetLegacy Universe.SqlInsights.NetCore Universe.SqlInsights.Shared Universe.SqlInsights.SqlServerStorage) DO (
  rd /q /s src\%%d\bin
  rd /q /s src\%%d\obj
)

pushd src\
nuget restore -verbosity quiet
for %%c in (Release Debug) Do (
  msbuild /t:Restore,Rebuild /v:m /p:Configuration=%%c /p:Version=%VER% /p:VersionSuffix=beta Universe.SqlInsights.sln
)
rem dotnet build /v:m /p:Version=%VER% Universe.SqlInsights.sln
popd

