test ! -d ~/source/sqlinsights && git clone https://github.com/devizer/Universe.SqlInsights ~/source/sqlinsights

cd ~/source/sqlinsights/src/Universe.SqlInsights.W3Api
git pull
time dotnet build -c Release
export ConnectionStrings__SqlInsights="Server=192.168.213.2;Database=SqlInsights_v4;User ID=sa;Password=\`1qazxsw2"
export ConnectionStrings__SqlInsights="Server=192.168.0.42;Database=SqlInsights_v4;User ID=sa;Password=\`1qazxsw2"
nohup dotnet run -c Release 2>&1 > ~/w3api.log &

cd ~/source/sqlinsights/src/universe.sqlinsights.w3app
yarn install 
yarn build
export PORT=6060
cd build
nohup npx serve &
tail -f ~/w3api.log



