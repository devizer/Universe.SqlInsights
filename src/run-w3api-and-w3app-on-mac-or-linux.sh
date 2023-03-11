function installRuntime() {
  curl -ksSL -o /tmp/a https://dot.net/v1/dotnet-install.sh; bash /tmp/a --runtime aspnetcore --channel 5.0 -i /opt/asptnet5
}

pkill dotnet; pkill node; 
cd ~; rm -rf ~/source/sqlinsights
test ! -d ~/source/sqlinsights && git clone https://github.com/devizer/Universe.SqlInsights ~/source/sqlinsights

export ConnectionStrings__SqlInsights="Server=192.168.0.42;Database=SqlInsights_v4;User ID=sa;Password=\`1qazxsw2"
export SQLINSIGHTS_CONNECTION_STRING="$ConnectionStrings__SqlInsights"
cd ~/source/sqlinsights/src/Universe.SqlInsights.SqlServerStorage.Tests; git pull; dotnet test -f net5.0 -c Release


cd ~/source/sqlinsights/src/Universe.SqlInsights.W3Api
pkill dotnet; git pull; sleep 4; nohup dotnet run -c Release -p:Version=9.9.9.9 2>&1 > ~/w3api.log &

if [[ "$(uname -s)" == Darwin ]]; then
  cd ~/source/sqlinsights/src/universe.sqlinsights.w3app
  yarn install 
  yarn build
  export PORT=6060
  cd build
  nohup npx serve &
fi

tail -f ~/w3api.log


