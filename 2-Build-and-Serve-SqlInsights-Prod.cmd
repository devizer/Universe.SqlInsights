Set PORT=6060
pushd src\universe.sqlinsights.w3app
If Not Exist node_modules (call yarn install)
call yarn build
pushd build
start /max "Universe.SqlTrace" cmd /c npx serve
popd
popd
echo WAITING FOR W3API......
ping -n 10 localhost > nul
start http://localhost:%PORT%