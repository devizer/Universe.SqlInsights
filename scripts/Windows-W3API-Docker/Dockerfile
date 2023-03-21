ARG TAG
FROM mcr.microsoft.com/windows/nanoserver:$TAG

COPY . /sqlinsights
WORKDIR /sqlinsights
ENV ASPNETCORE_URLS http://*:80

CMD Universe.SqlInsights.W3Api.exe

