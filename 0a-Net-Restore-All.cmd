pushd src\
for %%d in (Universe.SqlInsights.AspNetLegacy Universe.SqlInsights.NetCore Universe.SqlInsights.Shared Universe.SqlInsights.SqlServerStorage Universe.SqlInsights.NUnit) DO (
  echo Clean src\%%d\bin
  if exist src\%%d\bin rd /q /s src\%%d\bin
  echo Clean src\%%d\obj
  if exist src\%%d\obj rd /q /s src\%%d\obj
)

Echo dotnet Restore
dotnet restore /v:q
Echo msbuild Restore
msbuild /t:Restore /v:m
Echo Nuget Restore
nuget restore -verbosity quiet
popd

