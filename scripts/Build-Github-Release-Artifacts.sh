set -eu; set -o pipefail
pushd src/Universe.SqlInsights.W3Api
public=$(pwd)/bin/public
mkdir -p "$public"
n=0
for r in osx-x64 win-x64 win-x86 win-arm64 win-arm linux-x64 linux-arm linux-arm64 linux-musl-x64; do
  n=$((n++))
  Say "#${n}: BUILD [$r]"
  dotnet publish --self-contained -r $r -f net6.0 -o bin/plain/$r -v:q
  pushd bin/plain/$r
    if [[ "$r" == "win"* ]]; then
      7z a -tzip -mx=9 "$public"/sqlinsights-$r.zip *
      7z a -t7z -mx=9 -mq=on -mqs=on "$public"/sqlinsights-$r.7z *
    else
      tar cf - . | xz -9 -e -z > "$public"/sqlinsights-$r.tar.xz
      tar cf - . | gzip -9 -c  > "$public"/sqlinsights-$r.tar.gz
    fi
  popd
done

cp -r -a "$public" "$SYSTEM_ARTIFACTSDIRECTORY"/
