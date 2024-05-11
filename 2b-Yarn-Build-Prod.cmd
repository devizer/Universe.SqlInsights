pushd src\universe.sqlinsights.w3app
If Not Exist node_modules (call yarn install)
rem call yarn test
call yarn build
popd