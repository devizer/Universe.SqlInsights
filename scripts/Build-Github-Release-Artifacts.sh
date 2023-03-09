set -eu; set -o pipefail
pushd src/Universe.SqlInsights.W3Api
public=$(pwd)/bin/public
prefix="sqlinsights-w3api"
mkdir -p "$public"
n=0
for r in osx-x64 win-x64 win-x86 win-arm64 win-arm linux-x64 linux-arm linux-arm64 linux-musl-x64; do
  n=$((n+1))
  Say "#${n}: BUILD [$r]"
  dotnet publish --self-contained -r $r -f net6.0 -o bin/plain/$r -v:q
  pushd bin/plain/$r
    if [[ "$r" == "win"* ]]; then
      7z a -tzip -mx=9 "$public"/$prefix-$r.zip *
      7z a -t7z -mx=9 -ms=on -mqs=on "$public"/$prefix-$r.7z *
    else
      tar cf - . | xz -9 -e -z -T0 > "$public"/$prefix-$r.tar.xz
      tar cf - . | gzip -9 -c  > "$public"/$prefix-$r.tar.gz
    fi
  popd
done

cp -r -a "$public" "$SYSTEM_ARTIFACTSDIRECTORY"/

cd $BUILD_REPOSITORY_LOCALPATH/src/universe.sqlinsights.w3app/build
7z a -mx=9 -ms=on mqs=on "$SYSTEM_ARTIFACTSDIRECTORY"/w3app.7z .

