FROM debian:bullseye-slim

ARG SQLINSIGHTS_VERSION

RUN set -eu; export SQLINSIGHTS_VERSION; \
  apt-get update -qq; apt-get install sudo curl -y -qq; \
  mkdir -p /opt/sqlinsights-dashboard; echo $SQLINSIGHTS_VERSION > /opt/sqlinsights-dashboard/VERSION; \
  url=https://raw.githubusercontent.com/devizer/glist/master/install-dotnet-dependencies.sh; (curl -ksSL $url) | UPDATE_REPOS="" bash -e && echo "Successfully installed .NET Core Dependencies"; \
  curl -ksSL -o /tmp/a https://dot.net/v1/dotnet-install.sh; \
  for v in 2.1 2.2 3.0 3.1 5.0 6.0 7.0; do \
    echo "INSTALLING ASPNET CORE RUNTIME [$v]"; \
    bash /tmp/a --runtime aspnetcore --channel $v -i /usr/share/dotnet; \
  done; \
  suffix="arm"; arch="$(dpkg --print-architecture)"; if [ "$arch" = "amd64" ]; then suffix="x64"; fi; if [ "$arch" = "arm64" ]; then suffix="arm64"; fi; \
  echo "uname -m: [$(uname -m)]. ARCH: [$arch]. SUFFIX: [$suffix]"; \
  url="https://github.com/devizer/Universe.SqlInsights/releases/download/$SQLINSIGHTS_VERSION/sqlinsights-dashboard-fxdependent.tar.gz"; \
  echo "DOWNLOADING [$url]"; \
  curl -kSL -o /tmp/gz.gz "$url"; \
  tar xzf /tmp/gz.gz -C /opt/sqlinsights-dashboard; \
  rm -rf /tmp/* /var/cache/apt/* /var/lib/apt/*; \
  echo "[FX Dependent] DOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOONE: [$suffix]"

CMD set -eu; \
  test -s /opt/sqlinsights-dashboard/VERSION && echo "SqlInsights Dashboard $(cat /opt/sqlinsights-dashboard/VERSION) is starting ..."; \
  cd /opt/sqlinsights-dashboard; \
  ASPNETCORE_URLS=http://0.0.0.0:80 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 /usr/share/dotnet/dotnet Universe.SqlInsights.W3Api.dll
