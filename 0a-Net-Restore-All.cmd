pushd src\
Echo dotnet Restore
dotnet restore /v:q
Echo msbuild Restore
msbuild /t:Restore /v:m
Echo Nuget Restore
nuget restore -verbosity quiet
popd

