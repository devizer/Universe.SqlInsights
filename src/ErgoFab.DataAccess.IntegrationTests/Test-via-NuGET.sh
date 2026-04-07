function Get-Nuget-package-Latest-Version() {
  pushd /tmp >/dev/null
  local folder="console$RANDOM"
  dotnet new console -o "$folder" >/dev/null; 
  cd $folder
  Say "Query package [$1] version" >&2
  time dotnet add package "$1" >/dev/null
  cat *.csproj | grep PackageReference | grep -Eo "([0-9]{1,}\.)+[0-9]{1,}" | tail -1
  cd ..
  rm -rf $folder >/dev/null 2>&1
  popd >/dev/null
}

function find-build-number() {
  local fullVersion="$(Get-Nuget-package-Latest-Version "Universe.SqlInsights.NetCore")"
  echo "$fullVersion" | grep -Eo "[0-9]{1,7}" | tail -1
}
prj=ErgoFab.DataAccess.IntegrationTests.csproj
build_number="$(find-build-number)"
echo "BUILD NUMBER = [$build_number]"
netCoreBuildVersion="$(Get-Nuget-package-Latest-Version "Universe.SqlInsights.NetCore")"
echo "Universe.SqlInsights.NetCore latest release = [$(Get-Nuget-package-Latest-Version "Universe.SqlInsights.NetCore")]"
echo "Universe.SqlInsights.NUnit   latest release = [$(Get-Nuget-package-Latest-Version "Universe.SqlInsights.NUnit")]"
ver="3.14.0.$build_number"
sed -i '/Universe.NUnitPipeline.SqlServerDatabaseFactory.csproj/d' "$prj"
sed -i '/Universe.SqlInsights.NUnit.csproj/d' "$prj"

Say "Add Package [Universe.NUnitPipeline.SqlServerDatabaseFactory]"
dotnet add "$prj" package Universe.NUnitPipeline.SqlServerDatabaseFactory -v "$ver" --no-restore
Say "Add Package [Universe.SqlInsights.NUnit]"
dotnet add "$prj" package Universe.SqlInsights.NUnit -v "$ver" --no-restore
echo "FINAL [$prj]"
cat "$prj"
time dotnet build -c Release -f net8.0
Say "TESTING ...."
dotnet test -c Release -f net8.0
Say "Finish tests"
