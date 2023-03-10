FROM debian:bullseye-slim

ARG SQLINSIGHTS_VERSION

RUN set -eu; export SQLINSIGHTS_VERSION; \
  apt-get update -qq; apt-get install sudo curl -y -qq; \
  mkdir -p /opt/sqlinsights-dashboard; echo $SQLINSIGHTS_VERSION > /opt/sqlinsights-dashboard/VERSION; \
  url=https://raw.githubusercontent.com/devizer/glist/master/install-dotnet-dependencies.sh; (curl -ksSL $url) | UPDATE_REPOS="" bash -e && echo "Successfully installed .NET Core Dependencies"; \
  curl -ksSL -o /tmp/a https://dot.net/v1/dotnet-install.sh; \
  for v in 5.0; do \
    echo "INSTALLING NET SDK RUNTIME [$v]"; \
    bash /tmp/a --channel $v -i /usr/share/dotnet || true; \
  done; \
  \
  echo "Downloading Source"; \
  curl -ksSL -o /tmp/src.tar.gz "https://codeload.github.com/devizer/Universe.SqlInsights/tar.gz/refs/tags/$SQLINSIGHTS_VERSION"; \
  mkdir -p /opt/src; tar xzf /tmp/src.tar.gz -C /opt/src; \
  echo "CONTENT OF the [/opt/src]"; ls -laR /opt/src; \
  cd /opt/src/src/Universe.SqlInsights.W3Api; \
  echo "RESTOOOOORING ..."; dotnet restore; \
  dotnet build -c Release -f net5.0; \
  \
  suffix="arm"; arch="$(dpkg --print-architecture)"; if [ "$arch" = "amd64" ]; then suffix="x64"; fi; if [ "$arch" = "arm64" ]; then suffix="arm64"; fi; \
  echo "uname -m: [$(uname -m)]. ARCH: [$arch]. SUFFIX: [$suffix]"; \
  rm -rf /tmp/* /var/cache/apt/* /var/lib/apt/*; \
  echo "[FX Dependent] DOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOONE: [$suffix]"

CMD set -eu; \
  test -s /opt/sqlinsights-dashboard/VERSION && echo "SqlInsights Dashboard $(cat /opt/sqlinsights-dashboard/VERSION) is starting ..."; \
  cd /opt/src/src/Universe.SqlInsights.W3Api; \
  ASPNETCORE_URLS=http://0.0.0.0:80 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 /usr/share/dotnet/dotnet run --no-build --no-launch-profile
