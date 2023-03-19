set LocalLogsFolder__Windows=C:\ZZZ\SqlInsights Logs 666
@pushd src\Universe.SqlInsights.W3Api
start /max "W3API" cmd /c "chcp 65001 && dotnet run | "C:\Program Files\Git\usr\bin\tee.exe" w3api.log.tmp"
start /max http://localhost:50420/swagger
@popd