set -eu; set -o pipefail

rm -rf /tmp/src-copy || true
git clone "$BUILD_REPOSITORY_URI" /tmp/src-copy
pushd /tmp/src-copy
revision="$(set TZ=GMT; git log -n 999999 --date=raw --pretty=format:"%cd" | wc -l)"
popd
version="0.0.${revision}"
export SQLINSIGHTS_VERSION="v${version}"
export SQLINSIGHTS_VERSION_SHORT="$version"

echo "##vso[task.setvariable variable=SQLINSIGHTS_VERSION]$SQLINSIGHTS_VERSION"
echo "##vso[task.setvariable variable=SQLINSIGHTS_VERSION_SHORT]$SQLINSIGHTS_VERSION_SHORT"

Say "SQLINSIGHTS_VERSION: [$SQLINSIGHTS_VERSION]"
Say "SQLINSIGHTS_VERSION_SHORT: [$SQLINSIGHTS_VERSION_SHORT]"

