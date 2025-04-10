set LocalLogsFolder__Windows=C:\Temp\SqlInsights Custom Logs
@pushd src\Universe.SqlInsights.W3Api
rem start /max "W3API" cmd /c "chcp 65001 && dotnet run | "C:\Program Files\Git\usr\bin\tee.exe" w3api.log.tmp"
start /min "W3API" cmd /c "chcp 65001 && dotnet run --no-build -c Debug
start /max http://localhost:50420/swagger
@popd