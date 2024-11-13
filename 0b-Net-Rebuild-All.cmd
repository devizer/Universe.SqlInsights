taskkill /F /T /IM chromedriver.exe
If Not Defined VER Set VER=4.3.2.1

pushd src\
for %%c in (Debug) Do (
  echo MSBuild %%c
  msbuild /t:Build /v:m /p:Configuration=%%c /p:PackageVersion=%VER% /p:Version=%VER% /p:VersionSuffix=beta Universe.SqlInsights.sln
)
rem dotnet build /v:m /p:Version=%VER% Universe.SqlInsights.sln
popd

