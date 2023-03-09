set -eu; set -o pipefail

if [[ "$(command -v pigz)" == "" ]]; then Say "Install pigz"; sudo apt-get update -y -qq; sudo apt-get install pigz -y -qq; fi

function Filter-7z() {
  grep "archive\|bytes" || true;
}

Say "Grap universe.sqlinsights.w3app"
pushd $BUILD_REPOSITORY_LOCALPATH/src/universe.sqlinsights.w3app/build
pwd
7z a -mx=9 -ms=on -mqs=on "$SYSTEM_ARTIFACTSDIRECTORY"/w3app.7z . | Filter-7z
popd

pushd src/Universe.SqlInsights.W3Api
public=$(pwd)/bin/public
prefix="sqlinsights-w3api"
mkdir -p "$public"
n=0
for r in osx-x64 osx-arm64 win-x64 win-x86 win-arm64 win-arm linux-x64 linux-arm linux-arm64 linux-musl-x64 osx.10.10-x64 osx.10.11-x64; do
  n=$((n+1))
  Say "#${n}: BUILD [$r]"
  dotnet publish --self-contained -r $r -f net6.0 -o bin/plain/$r -v:q
  pushd bin/plain/$r
    chmod 644 *.dll
    test -s Universe.SqlInsights.W3Api && chmod 755 Universe.SqlInsights.W3Api
    if [[ "$r" == "win"* ]]; then
      time 7z a -tzip -mx=9 "$public"/$prefix-$r.zip * | Filter-7z
      time 7z a -t7z -mx=9 -ms=on -mqs=on "$public"/$prefix-$r.7z * | Filter-7z
    else
      # time tar cf - . | xz -9 -e -z -T0 > "$public"/$prefix-$r.tar.xz
      time tar cf - . | pigz -p $(nproc) -b 128 -9  > "$public"/$prefix-$r.tar.gz
      time tar cf - . | 7za a dummy -txz -mx=9 -si -so > "$public"/$prefix-$r.tar.xz
      # pigz -p 8 -b 128 -9
      # gzip -9 -c
    fi
  popd
done

cp -r -a "$public" "$SYSTEM_ARTIFACTSDIRECTORY"/
