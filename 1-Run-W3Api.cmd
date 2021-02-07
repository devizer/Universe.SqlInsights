@pushd src\Universe.SqlInsights.W3Api
start /max "W3API" dotnet run
start /max http://localhost:50420/swagger
@popd