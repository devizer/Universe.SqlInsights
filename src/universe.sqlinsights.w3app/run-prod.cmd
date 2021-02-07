call yarn build
pushd build
start /max "Universe.SqlTrace" cmd /c npx serve
popd