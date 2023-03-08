#!/usr/bin/env bash
set -eu; set -o pipefail
set -e


# Configure Docker
docker image rm -f devizervlad/sqlinsights-dashboard:latest || true
docker run --rm --privileged multiarch/qemu-user-static --reset -p yes
sudo apt-get install qemu-user-static -y

docker buildx inspect --bootstrap
docker buildx ls

# BUILD W3APP APP
script=https://raw.githubusercontent.com/devizer/test-and-build/master/install-build-tools-bundle.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash >/dev/null
export NODE_VER=v14.19.1 SKIP_NPM_UPGRADE=True
time (script=https://raw.githubusercontent.com/devizer/glist/master/install-dotnet-and-nodejs.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash -s node)
dir="$(pwd)"
pushd ../../src/universe.sqlinsights.w3app
time yarn install 
time yarn build
cp -f -a build "$dir"


docker_version="$(date +%F)"
Say "Docker Image version: [${docker_version}]"
# docker image rm -f $(docker image ls -aq)
export OS=Linux
export TAGS="-t devizervlad/sqlinsights-dashboard:v${docker_version} -t devizervlad/sqlinsights-dashboard:latest"
export BASE_IMAGE='nginx:latest'
platform="linux/amd64,linux/arm32v5,linux/arm32v6,linux/arm32v7,linux/arm64v8,linux/i386,linux/mips64le,linux/ppc64le,linux/s390x"

# revert to --push
time docker buildx build \
  --build-arg BASE_IMAGE="${BASE_IMAGE}" \
  --build-arg BUILD_URL="${BUILD_URL}" \
  --build-arg JOB_URL="${JOB_URL}" \
  --build-arg BUILD_SOURCEVERSION="${BUILD_SOURCEVERSION}" \
  --build-arg BUILD_SOURCEBRANCHNAME="${BUILD_SOURCEBRANCHNAME}" \
  --build-arg BUILD_BUILDID="${BUILD_BUILDID}" \
  --platform $platform --load \
  ${TAGS} .

Say "Built with --load only"

# docker run --restart on-failure --name agent007 --privileged --hostname agent007 -it devizervlad/azpa:latest 
