taskkill /F /T /IM chromedriver.exe
msbuild /t:build
bin\Debug\AdventureWorks.HeadlessTests.exe 