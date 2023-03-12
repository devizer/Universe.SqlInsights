FROM mcr.microsoft.com/dotnet/aspnet:5.0
# v5 - debian 10

ARG SQLINSIGHTS_VERSION

RUN set -eu; export SQLINSIGHTS_VERSION; \
  apt-get update -qq; apt-get install sudo curl -y -qq; \
  mkdir -p /opt/sqlinsights-dashboard; echo $SQLINSIGHTS_VERSION > /opt/sqlinsights-dashboard/VERSION; \
  suffix="arm"; arch="$(dpkg --print-architecture)"; if [ "$arch" = "amd64" ]; then suffix="x64"; fi; if [ "$arch" = "arm64" ]; then suffix="arm64"; fi; \
  echo "uname -m: [$(uname -m)]. ARCH: [$arch]. SUFFIX: [$suffix]"; \
  url="https://github.com/devizer/Universe.SqlInsights/releases/download/$SQLINSIGHTS_VERSION/sqlinsights-dashboard-fxdependent.tar.gz"; \
  echo "DOWNLOADING [$url]"; \
  curl -kSL -o /tmp/app.gz "$url"; \
  tar xzf /tmp/app.gz -C /opt/sqlinsights-dashboard; \
  rm -rf /tmp/* /var/cache/apt/* /var/lib/apt/*; \
  echo "[FX Dependent] DOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOONE: [$suffix]"

# DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 is unsupported for Microsoft DbProviderFactory
CMD set -eu; \
  test -s /opt/sqlinsights-dashboard/VERSION && echo "SqlInsights Dashboard $(cat /opt/sqlinsights-dashboard/VERSION) is starting ..."; \
  cd /opt/sqlinsights-dashboard; \
  ASPNETCORE_URLS=http://0.0.0.0:80 dotnet Universe.SqlInsights.W3Api.dll
