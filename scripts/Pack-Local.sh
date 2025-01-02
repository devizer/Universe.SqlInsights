revision=$(git log -n 999999 --date=raw --pretty=format:"%cd" | wc -l)
export SQLINSIGHTS_VERSION_SHORT="2.8.$revision"
echo "SQLINSIGHTS_VERSION_SHORT = [$SQLINSIGHTS_VERSION_SHORT]"
# dotnet pack -c Release -p:PackageVersion=$SQLINSIGHTS_VERSION_SHORT -p:Version=$SQLINSIGHTS_VERSION_SHORT -p:IncludeSymbols=True -p:SymbolPackageFormat=snupkg

mkdir -p ../bin
binDir="$PWD/../bin"

pushd ../src

for p in Universe.SqlInsights.W3Api.Client Universe.SqlInsights.NUnit Universe.NUnitPipeline.SqlServerDatabaseFactory Universe.SqlInsights.Shared Universe.SqlInsights.GenericInterceptor Universe.SqlInsights.NetCore Universe.SqlInsights.W3Api Universe.SqlInsights.SqlServerStorage; do
  cd $p
  # dotnet pack -c Release -p:PackageVersion=$SQLINSIGHTS_VERSION_SHORT -p:Version=$SQLINSIGHTS_VERSION_SHORT -p:IncludeSymbols=True -p:SymbolPackageFormat=snupkg
  dotnet build -c Release -p:PackageVersion=$SQLINSIGHTS_VERSION_SHORT -p:Version=$SQLINSIGHTS_VERSION_SHORT -p:IncludeSymbols=True -p:SymbolPackageFormat=snupkg
  cd ..
done

find -name '*.nupkg' | grep '/Release/' | grep "$SQLINSIGHTS_VERSION_SHORT" | while IFS='' read -r nupkg; do
  echo "UNPACK [$nupkg]"
  tmpUnpack=$binDir/temp
  7z x -y $nupkg -o"$tmpUnpack/" >/dev/null
  cp -v $nupkg $binDir/
done

popd
