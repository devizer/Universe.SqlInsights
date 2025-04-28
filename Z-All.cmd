call 0a-Net-Restore-All.cmd 
call 0b-Net-Rebuild-All.cmd 
call 1-Run-AdventureWorks.cmd 
call 1-Run-W3Api.cmd 
call 2c-Serve-SqlInsights-w3app.cmd 
call 3-Run-AdventureWorks-Headless-Test.cmd 
start 1b-FakeError.html
rem call 3-Run-AdventureWorks-Headless-Test.cmd 
rem call 3-Run-AdventureWorks-Headless-Test.cmd 
call 4-Run-ErgoFab-Integration-Tests.cmd 
