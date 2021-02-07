pushd "%LOCALAPPDATA%"
echo [System.DateTime]::Now.ToString("yyyy-MM-dd,HH-mm-ss") | powershell -command - > "%LOCALAPPDATA%\.backup.timestamp"
for /f %%i in (%LOCALAPPDATA%\.backup.timestamp) do set datetime=%%i
popd

rem MAX: -mx=9 -mfb=128 -md=128m
"C:\Program Files\7-Zip\7zG.exe" a -t7z -mx=9 -ms=on -mqs=on -xr!.git -xr!bin -xr!obj -xr!packages -xr!.vs -xr!build -xr!dist -xr!node_modules ^
  "C:\Users\Backups on Google Drive\Universe.SqlInsights (%datetime%).7z" .
