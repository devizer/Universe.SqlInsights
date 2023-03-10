#!/usr/bin/env bash
set -eu; set -o pipefail

TARGET_IMAGE="sqlinsights-dashboard-fxdependent"
platform="linux/amd64,linux/arm/v7,linux/arm64"

export TAGS="-t devizervlad/$TARGET_IMAGE:${SQLINSIGHTS_VERSION} -t devizervlad/$TARGET_IMAGE:latest"

Say "BUILD ALL THE PLATFORMS"
# revert to --push
time docker buildx build \
  --build-arg SQLINSIGHTS_VERSION="${SQLINSIGHTS_VERSION}" \
  --platform $platform --push \
  ${TAGS} .

# Say "Built with --load only"

docker image ls

# docker run --restart on-failure --name agent007 --privileged --hostname agent007 -it devizervlad/azpa:latest 
