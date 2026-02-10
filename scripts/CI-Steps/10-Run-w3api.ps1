cd C:\App\Goods\w3api
$ENV:ASPNETCORE_URLS="http://*:50420"
Start-Process "dotnet" -ArgumentList "Universe.SqlInsights.W3Api.dll".Split(" ") -NoNewWindow
Sleep 60
