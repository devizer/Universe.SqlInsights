FROM mcr.microsoft.com/dotnet/aspnet:6.0
# v5 - debian 10, v6 - debian 11

ARG SQLINSIGHTS_VERSION
COPY build /opt/sqlinsights-dashboard

# DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 is unsupported for Microsoft DbProviderFactory
# LC_ALL="C.UTF-8" is available on DXDependent
CMD set -eu; \
  test -s /opt/sqlinsights-dashboard/VERSION && echo "SqlInsights Dashboard $(cat /opt/sqlinsights-dashboard/VERSION) is starting ..."; \
  cd /opt/sqlinsights-dashboard; \
  ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://0.0.0.0:80}" dotnet Universe.SqlInsights.W3Api.dll
