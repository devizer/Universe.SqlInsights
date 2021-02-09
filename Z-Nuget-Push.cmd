SET VER=0.3.1.0
call 0-Rebuild-All.cmd 
for %%d in (Universe.SqlInsights.Shared Universe.SqlInsights.SqlServerStorage Universe.SqlInsights.AspNetLegacy Universe.SqlInsights.NetCore) DO (
  pushd src\%%d\bin\Release
  nuget push *.nupkg %NUGET_BUILD_SERVER% -Timeout 600 -Source https://www.nuget.org/api/v2/package
  popd
)

