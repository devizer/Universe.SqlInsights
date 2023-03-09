FROM debian:bullseye-slim

ARG SQLINSIGHTS_VERSION

RUN set -eu; export SQLINSIGHTS_VERSION; \
  apt-get update -qq; apt-get install sudo curl -y -qq; \
  url=https://raw.githubusercontent.com/devizer/glist/master/install-dotnet-dependencies.sh; (curl -ksSL $url) | UPDATE_REPOS="" bash -e && echo "Successfully installed .NET Core Dependencies"; \
  suffix="arm"; arch="$(dpkg --print-architecture)"; if [ "$arch" = "amd64" ]; then suffix="x64"; fi; if [ "$arch" = "arm64" ]; then suffix="arm64"; fi; \
  echo "uname -m: [$(uname -m)]. ARCH: [$arch]. SUFFIX: [$suffix]"; \
  url="https://github.com/devizer/Universe.SqlInsights/releases/download/$SQLINSIGHTS_VERSION/sqlinsights-dashboard-linux-${suffix}.tar.gz"; \
  echo "DOWNLOADING [$url]"; \
  curl -kSL -o /tmp/gz.gz "$url"; \
  mkdir -p /opt/sqlinsights-dashboard; tar xzf /tmp/gz.gz -C /opt/sqlinsights-dashboard; \
  rm -rf /tmp/* /var/cache/apt/* /var/lib/apt/*; \
  echo "DOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOONE: [$suffix]"

CMD set -eu; echo "SqlInsights Dashboard $SQLINSIGHTS_VERSION is starting ..."; \
  cd /opt/sqlinsights-dashboard; \
  ASPNETCORE_URLS=http://0.0.0.0:80 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 ./Universe.SqlInsights.W3Api
