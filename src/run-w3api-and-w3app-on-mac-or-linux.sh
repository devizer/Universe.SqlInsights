cd ~; rm -rf ~/source/sqlinsights
test ! -d ~/source/sqlinsights && git clone https://github.com/devizer/Universe.SqlInsights ~/source/sqlinsights

export ConnectionStrings__SqlInsights="Server=192.168.0.42;Database=SqlInsights_v4;User ID=sa;Password=\`1qazxsw2"
export SQLINSIGHTS_CONNECTION_STRING="$ConnectionStrings__SqlInsights"
cd ~/source/sqlinsights/src/Universe.SqlInsights.SqlServerStorage.Tests; dotnet test -f net5.0 -c Release


cd ~/source/sqlinsights/src/Universe.SqlInsights.W3Api
git pull
time dotnet build -c Release
export ConnectionStrings__SqlInsights="Server=192.168.213.2;Database=SqlInsights_v4;User ID=sa;Password=\`1qazxsw2"
nohup dotnet run -c Release 2>&1 > ~/w3api.log &

if [[ "$(uname -s)" == Darwin ]]; then
  cd ~/source/sqlinsights/src/universe.sqlinsights.w3app
  yarn install 
  yarn build
  export PORT=6060
  cd build
  nohup npx serve &
fi

tail -f ~/w3api.log


