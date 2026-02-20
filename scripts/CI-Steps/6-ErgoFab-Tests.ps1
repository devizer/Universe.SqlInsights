. "$PSScriptRoot\Functions.ps1"
$ENV:NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES = "True"

# if [[ "${RAM_DISK:-}" == "" ]]; then export NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES=True; fi # for query cache
cd Goods\ergofab.tests
# TODO: Authentication=SqlPassword
& dotnet test ErgoFab.DataAccess.IntegrationTests.dll 2>&1 | Tee-Object "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\ErgoFab.DataAccess.IntegrationTests.dll.log"

echo "Copying .\TestsOutput to '$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\' ..."
Copy-Item -Path ".\TestsOutput" -Destination "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\" -Recurse -Force

Show-Last-Exit-Code "TEST ErgoFab.DataAccess.IntegrationTests.dll" -Throw
