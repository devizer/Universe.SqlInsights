pushd ..
dotnet build -c Release -f net6.0 -v:q
set ERGOFAB_TEST_BINARY_FOLDER=%CD%\bin\Release\net6.0
popd

echo %ERGOFAB_TEST_BINARY_FOLDER%
powershell -f test-on-all-servers.ps1 