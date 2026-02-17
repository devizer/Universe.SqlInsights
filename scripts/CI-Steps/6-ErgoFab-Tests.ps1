. "$PSScriptRoot\Functions.ps1"
$ENV:NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES = "True"

# if [[ "${RAM_DISK:-}" == "" ]]; then export NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES=True; fi # for query cache
cd Goods\ergofab.tests
# export ERGOFAB_TESTS_DATA_FOLDER="D:\\ErgFab-Tests"
$ENV:ERGOFAB_TESTS_HISTORY_CONNECTIONSTRING="Server=$ENV:SQL_INSTANCE_NAME;Encrypt=False;Initial Catalog=SqlInsights Local Warehouse;Integrated Security=SSPI"
$ENV:ERGOFAB_TESTS_REPORT_FULLNAME="$SYSTEM_ARTIFACTSDIRECTORY\ErgFab Tests Report.txt"

& dotnet test ErgoFab.DataAccess.IntegrationTests.dll 2>&1 | Tee-Object "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\ErgoFab.DataAccess.IntegrationTests.dll.log"
Show-Last-Exit-Code "TEST ErgoFab.DataAccess.IntegrationTests.dll"

echo "Copying .\TestsOutput to '$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\' ..."
Copy-Item -Path ".\TestsOutput" -Destination "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\" -Recurse -Force
