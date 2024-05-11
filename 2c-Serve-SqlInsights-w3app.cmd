Set PORT=6060
pushd src\universe.sqlinsights.w3app\build
dotnet serve --version 2>nul || dotnet tool install --global dotnet-serve
rem start /max "w3app" cmd /c npx serve
start /max "w3app" dotnet serve -p %PORT% -o
popd
