FROM debian:bullseye-slim

ARG SQLINSIGHTS_VERSION

# url=https://raw.githubusercontent.com/devizer/glist/master/install-dotnet-dependencies.sh; (curl -ksSL $url) | UPDATE_REPOS="" bash -e && echo "Successfully installed .NET Core Dependencies"; \
# libkrb5-3 libgssapi-krb5-2 zlib1g libicu67 libssl1.1 liblttng-ust0
RUN set -eu; export SQLINSIGHTS_VERSION; \
  apt-get update -qq; apt-get install -y -qq --no-install-recommends sudo curl libkrb5-3 libgssapi-krb5-2 zlib1g libicu67 libssl1.1 liblttng-ust0; \
  mkdir -p /opt/sqlinsights-dashboard; echo $SQLINSIGHTS_VERSION > /opt/sqlinsights-dashboard/VERSION; \
  \
  suffix="arm"; arch="$(dpkg --print-architecture)"; if [ "$arch" = "amd64" ]; then suffix="x64"; fi; if [ "$arch" = "arm64" ]; then suffix="arm64"; fi; \
  echo "uname -m: [$(uname -m)]. ARCH: [$arch]. SUFFIX: [$suffix]"; \
  url="https://github.com/devizer/Universe.SqlInsights/releases/download/$SQLINSIGHTS_VERSION/sqlinsights-dashboard-linux-${suffix}.tar.gz"; \
  echo "DOWNLOADING [$url]"; \
  curl -kSL -o /tmp/gz.gz "$url"; \
  tar xzf /tmp/gz.gz -C /opt/sqlinsights-dashboard; \
  rm -rf /tmp/* /var/cache/apt/* /var/lib/apt/*; \
  echo "[Self-Contained] DOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOONE: [$suffix]"

# DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 is not supported by Microsoft.Data.SqlClient
CMD set -eu; \
  test -s /opt/sqlinsights-dashboard/VERSION && echo "SqlInsights Dashboard $(cat /opt/sqlinsights-dashboard/VERSION) is starting ..."; \
  cd /opt/sqlinsights-dashboard; \
  if [ -n "${LOCAL_LOGS_FOLDER:-}" ]; then LocalLogsFolder__Unix="${LOCAL_LOGS_FOLDER:-}"; export LocalLogsFolder__Unix; else LocalLogsFolder__Enable=False; export LocalLogsFolder__Enable; fi; \
  ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://0.0.0.0:80}" ./Universe.SqlInsights.W3Api
