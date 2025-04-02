function find-build-number() {
  pushd /tmp >/dev/null
  dotnet new console -o console1; cd console1 >/dev/null
  dotnet add package Universe.SqlInsights.NUnit >/dev/null
  cat *.csproj | grep PackageReference | grep -Eo "[0-9]{1,7}" | tail -1
  cd ..
  rm -rf console1 >/dev/null 2>&1
  popd >/dev/null
}
prj=ErgoFab.DataAccess.IntegrationTests.csproj
build_number="$(find-build-number)"
echo "BUILD NUMBER = [$build_number]"
ver="3.14.0.$build_number"
sed -i '/Universe.NUnitPipeline.SqlServerDatabaseFactory.csproj/d' "$prj"
sed -i '/Universe.SqlInsights.NUnit.csproj/d' "$prj"
dotnet add "$prj" package Universe.NUnitPipeline.SqlServerDatabaseFactory -v "$ver"
dotnet add "$prj" package Universe.SqlInsights.NUnit -v "$ver"
dotnet test -c Release -f net8.0