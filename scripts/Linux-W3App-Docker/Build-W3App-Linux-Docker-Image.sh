#!/usr/bin/env bash
set -eu; set -o pipefail
set -e


Say "CONFIGURE DOCKER"
docker image rm -f devizervlad/sqlinsights-dashboard:latest || true
docker run --rm --privileged multiarch/qemu-user-static --reset -p yes
sudo apt-get install qemu-user-static -y

docker buildx create --name advancedx
docker buildx use advancedx
docker buildx inspect --bootstrap
Say "Supported architectures"
docker buildx ls

Say "BUILD W3APP APP"
script=https://raw.githubusercontent.com/devizer/test-and-build/master/install-build-tools-bundle.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash >/dev/null
export NODE_VER=v14.19.1 SKIP_NPM_UPGRADE=True
time (script=https://raw.githubusercontent.com/devizer/glist/master/install-dotnet-and-nodejs.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash -s node)
dir="$(pwd)"
pushd ../../src/universe.sqlinsights.w3app
time yarn install
time yarn build
cp -f -a build "$dir"
popd

Say "BUILD [config-w3app.sh]"
export INDEX_HTML=../../src/universe.sqlinsights.w3app/build/index.html
pwsh Build-NGINX-Startup.ps1
chmod +x config-w3app.sh || true
cat config-w3app.sh

Say "BUILD X64 ONLY CONTAINER"
time docker build -t w3app-x64 .
Say "TEST X64 ONLY CONTAINER"
docker run --name test-w3app -d -p 8080:80 -e SQL_INSIGHTS_W3API_URL="http://my.overridden.api:7654/api/vNext/" w3app-x64
sleep 3
echo CURL
curl -s -I http://localhost:8080 | cat
echo LOGS
docker logs test-w3app
Say "OVERRIDDEN /usr/share/nginx/html/index.html"
docker exec -t test-w3app cat /usr/share/nginx/html/index.html

Say "/etc/nginx"
docker cp test-w3app:/etc/nginx ~/etc-nginx/
7z a $SYSTEM_ARTIFACTSDIRECTORY/etc-nging.7z ~/etc-nginx/*

TARGET_IMAGE="sqlinsights-dashboard"
docker_version="$(date +%F)"
Say "Docker Image version: [${docker_version}]"
# docker image rm -f $(docker image ls -aq)
export OS=Linux
export TAGS="-t devizervlad/$TARGET_IMAGE:v${docker_version} -t devizervlad/$TARGET_IMAGE:latest"
export BASE_IMAGE='nginx:latest'
platform="linux/amd64,linux/arm32v5,linux/arm32v6,linux/arm32v7,linux/arm64v8,linux/i386,linux/mips64le,linux/ppc64le,linux/s390x"
platform="linux/amd64,linux/arm/v6,linux/arm/v7,linux/arm64,linux/386,linux/mips64le,linux/ppc64le,linux/s390x"
# linux/amd64, linux/amd64/v2, linux/amd64/v3, linux/arm64, linux/riscv64, linux/ppc64, linux/ppc64le, linux/s390x, linux/386, linux/mips64le, linux/mips64, linux/arm/v7, linux/arm/v6

Say "BUILD ALL THE PLATFORMS"
# revert to --push
time docker buildx build \
  --platform $platform --push \
  ${TAGS} .

# Say "Built with --load only"

docker image ls

# docker run --restart on-failure --name agent007 --privileged --hostname agent007 -it devizervlad/azpa:latest 
