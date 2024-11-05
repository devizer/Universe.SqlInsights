cd /opt/s4app
sqlServer=192.168.2.42
export ASPNETCORE_URLS="http://*:40080" 
export ConnectionStrings__SqlInsights="Data Source=$sqlServer; Initial Catalog=SqlInsights Local Warehouse; User ID=sa; password=\`1qazxsw2;TrustServerCertificate=True;Encrypt=False" 
./Universe.SqlInsights.W3Api

