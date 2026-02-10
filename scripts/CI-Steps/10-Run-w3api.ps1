cd C:\App\Goods\w3api
$ENV:ASPNETCORE_URLS="http://*:50420"
# Start-Process "dotnet" -ArgumentList "Universe.SqlInsights.W3Api.dll".Split(" ") -NoNewWindow
# & { dotnet Universe.SqlInsights.W3Api.dll } &
Smart-Start-Process "dotnet" "Universe.SqlInsights.W3Api.dll"

Sleep 60
