function installRuntime() {
  curl -ksSL -o /tmp/a https://dot.net/v1/dotnet-install.sh; bash /tmp/a --runtime aspnetcore --channel 5.0 -i /opt/asptnet5
}

pkill dotnet; pkill node; 
cd ~; rm -rf ~/source/sqlinsights
test ! -d ~/source/sqlinsights && git clone https://github.com/devizer/Universe.SqlInsights ~/source/sqlinsights

export ConnectionStrings__SqlInsights="TrustServerCertificate=True;Server=192.168.0.42;Database=SqlInsights vNext for $(hostname);User ID=sa;Password=\`1qazxsw2"
export SQLINSIGHTS_CONNECTION_STRING="$ConnectionStrings__SqlInsights"
cd ~/source/sqlinsights/src/Universe.SqlInsights.SqlServerStorage.Tests; git pull; dotnet test -f net5.0 -c Release


export ASPNETCORE_URLS=http://0.0.0.0:1234
cd ~/source/sqlinsights/src/Universe.SqlInsights.W3Api
pkill dotnet; git pull; sleep 4; nohup dotnet run -c Release --no-launch-profile -p:Version=9.9.9.9 2>&1 > ~/w3api.log &

if [[ "$(uname -s)" == Darwin ]]; then
  cd ~/source/sqlinsights/src/universe.sqlinsights.w3app
  yarn install 
  yarn build
  export PORT=6060
  cd build
  # nohup npx serve &
  dotnet serve --version 2>nul || dotnet tool install --global dotnet-serve
  dotnet serve --version
  nohup dotnet serve -p 6060 -o 2>&1 | tee ~/w3app.log &

fi

tail -f ~/w3api.log
