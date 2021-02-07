pushd src\
msbuild /t:Restore,Rebuild /v:m Universe.SqlInsights.sln
popd
