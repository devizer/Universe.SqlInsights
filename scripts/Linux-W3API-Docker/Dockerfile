FROM debian:bullseye-slim

ARG SQLINSIGHTS_VERSION

RUN set -eu; set -o pipefail; export SQLINSIGHTS_VERSION; \
  apt-get update -qq; apt-get install sudo curl -y -qq; \
  suffix="arm"; arch="$(dpkg --print-architecture)"; if [[ "$arch" == "amd64" ]]; then suffix="x64"; fi; if [[ "$arch" == "arm64" ]]; then suffix="arm64"; fi; \
  url="https://github.com/devizer/Universe.SqlInsights/releases/download/$SQLINSIGHTS_VERSION/sqlinsights-w3api-linux-${suffix}.tar.gz"; \
  echo "DOWNLOADING [$url]"; \
  curl -kSL -o /tmp/gz.gz "$url"; \
  mkdir -p /opt/sqlinsights-dashboard; tar xzf /tmp/gz.gz -C /opt/sqlinsights-dashboard; \
  rm -rf /tmp/* /var/cache/apt/* /var/lib/apt/*; \
  echo "DOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOONE"

CMD set -eu; cd /opt/sqlinsights-dashboard; ASPNETCORE_URLS=http://0.0.0.0:80; DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1; export ASPNETCORE_URLS; export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT; Universe.SqlInsights.W3Api
