set -eu; set -o pipefail

Say "COMPRESSION_LEVEL: [$COMPRESSION_LEVEL]"
Say "SHORT_ARTIFACT_RIDS: [${SHORT_ARTIFACT_RIDS:-}]"

RELEASE_NET=10.0
RELEASE_SUFFIX=
RELEASE_RIDS="osx-x64 osx-arm64 win-x64 win-x86 win-arm64 linux-x64 linux-arm linux-arm64 linux-musl-x64"
LEGACY_NET=6.0
LEGACY_SUFFIX=-legacy
LEGACY_RIDS="win-arm osx.10.10-x64 osx.10.11-x64"


if [[ "$(command -v pigz)" == "" ]]; then Say "Install pigz"; sudo apt-get update -y -qq; sudo apt-get install pigz -y -qq; fi

function Filter-7z() {
  grep "archive\|bytes" || true;
}

pushd src
Say "Remove 8.0, 9.0, and 10.0 Targets"
find . -name "*.csproj" | while IFS='' read -r csproj; do
  sed -i 's/net8.0;//g'  "$csproj"
  sed -i 's/net9.0;//g'  "$csproj"
  sed -i 's/net10.0;//g' "$csproj"
  echo ""; echo "FINAL [$csproj]"
  cat "$csproj"
done
cd Universe.SqlInsights.W3Api
public=$(pwd)/bin/public
mkdir -p "$public"
echo "$SQLINSIGHTS_VERSION" > "$public"/VERSION.txt

Say "Grab universe.sqlinsights.w3app"
pushd $BUILD_REPOSITORY_LOCALPATH/src/universe.sqlinsights.w3app/build
pwd
7z a -mx=9 "$public"/w3app.zip . | Filter-7z
time tar cf - . | pigz -p $(nproc) -b 128 -9  > "$public"/w3app.tar.gz
popd

prefix="sqlinsights-dashboard"

Say "BUILD FX DEPENDENT $SQLINSIGHTS_VERSION"
try-and-retry dotnet publish -f $W3API_NET -o bin/fxdepend -v:q -p:Version=$SQLINSIGHTS_VERSION_SHORT -c Release
mkdir -p bin/fxdepend/wwwroot; 
cp -r -a "$BUILD_REPOSITORY_LOCALPATH/src/universe.sqlinsights.w3app/build"/. bin/fxdepend/wwwroot
# SQL_INSIGHTS_W3API_URL_PLACEHOLDER --> /api/v1/SqlInsights
pushd bin/fxdepend
  sed -i 's/SQL_INSIGHTS_W3API_URL_PLACEHOLDER/\/api\/v1\/SqlInsights/g' wwwroot/index.html
  time tar cf - . | pigz -p $(nproc) -b 128 -${COMPRESSION_LEVEL}  > "$public"/$prefix-fxdependent.tar.gz
  time tar cf - . | 7za a dummy -txz -mx=${COMPRESSION_LEVEL} -si -so > "$public"/$prefix-fxdependent.tar.xz
  time 7z a -tzip -mx=${COMPRESSION_LEVEL} "$public"/$prefix-fxdependent.zip * | Filter-7z
  time 7z a -t7z -mx=${COMPRESSION_LEVEL} -ms=on -mqs=on "$public"/$prefix-fxdependent.7z * | Filter-7z
popd

n=0
# only net 6
if [[ -n "${SHORT_ARTIFACT_RIDS:-}" ]]; then
  rids="linux-x64 linux-arm linux-arm64 win-x64"
fi
# rids="linux-x64 linux-arm linux-arm64"
for kind in RELEASE LEGACY; do
NET="${kind}_NET"; NET="${!NET}"
SUFFIX="${kind}_SUFFIX"; SUFFIX="${!SUFFIX}"
RIDS="${kind}_RIDS"; RIDS="${!RIDS}"
Say "Building '$kind' Array: NET=[$NET], SUFFIX=[$SUFFIX], RIDS=[$RIDS]"
export DOTNET_VERSIONS=$NET DOTNET_TARGET_DIR=$HOME/DotNet.Custom/$NET SKIP_DOTNET_ENVIRONMENT=true
script=https://raw.githubusercontent.com/devizer/test-and-build/master/lab/install-DOTNET.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash;
for r in $rids; do
  n=$((n+1))
  Say "#${n}: BUILD SELF-CONTAINED [$r] $SQLINSIGHTS_VERSION"
  df -h -T
  try-and-retry $DOTNET_TARGET_DIR/dotnet publish --self-contained -r $r -f $W3API_NET -o bin/plain/$r -v:q -p:Version=$SQLINSIGHTS_VERSION_SHORT -c Release
  mkdir -p bin/plain/$r/wwwroot; cp -r -a "$BUILD_REPOSITORY_LOCALPATH/src/universe.sqlinsights.w3app/build"/. bin/plain/$r/wwwroot
  pushd bin/plain/$r
    sed -i 's/SQL_INSIGHTS_W3API_URL_PLACEHOLDER/\/api\/v1\/SqlInsights/g' wwwroot/index.html
    chmod 644 *.dll
    test -s Universe.SqlInsights.W3Api && chmod 755 Universe.SqlInsights.W3Api
    if [[ "$r" == "win"* ]]; then
      time 7z a -tzip -mx=${COMPRESSION_LEVEL} "$public"/$prefix-$r.zip * | Filter-7z
      time 7z a -t7z -mx=${COMPRESSION_LEVEL} -ms=on -mqs=on "$public"/$prefix-$r.7z * | Filter-7z
    else
      # time tar cf - . | xz -9 -e -z -T0 > "$public"/$prefix-$r.tar.xz
      time tar cf - . | pigz -p $(nproc) -b 128 -${COMPRESSION_LEVEL}  > "$public"/$prefix-$r.tar.gz
      time tar cf - . | 7za a dummy -txz -mx=${COMPRESSION_LEVEL} -si -so > "$public"/$prefix-$r.tar.xz
      # pigz -p 8 -b 128 -9
      # gzip -9 -c
    fi
  popd
  rm -rf bin/plain/$r
done
done

# HASH SUMS
function build_all_known_hash_sums() {
  pushd "$public"
  rm -f /tmp/hash-sums
  for file in *; do
    echo "HASH for '$file' in [$public]"
    for alg in md5 sha1 sha224 sha256 sha384 sha512; do
      if [[ "$(command -v ${alg}sum)" != "" ]]; then
        local sum=$(eval ${alg}sum "'"$file"'" | awk '{print $1}')
        echo "$file|$alg|$sum" >> /tmp/hash-sums
      else
        echo "warning! ${alg}sum missing"
      fi
    done
  done
  popd
  cp -f /tmp/hash-sums "$public"/hash-sums.txt
}

build_all_known_hash_sums

cp -r -a "$public" "$SYSTEM_ARTIFACTSDIRECTORY"/

if [[ "${SKIP_PUBLISH:-}" == "True" ]]; then echo SKIPPING PUBLISH. ENOUGH; exit 0; fi

Say "Create Github Release ${SQLINSIGHTS_VERSION}"
# "-p" option mean pre-release
gh release create -t "SqlInsights Dashboard Web API" -n "Ver ${SQLINSIGHTS_VERSION}" "$SQLINSIGHTS_VERSION" "$public"/*
popd
Say "Success. Complete."
df -h -T