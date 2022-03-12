@echo off
set TEMP=U:\TEMP
set TMP=U:\TEMP

set v=2019-DEV
set KEY=SQL-%v%
set NEW_SQL_INSTANCE_NAME=MSSQLSERVER
if Not Defined NEW_SQL_INSTANCE_NAME (
  set NEW_SQL_INSTANCE_NAME=DEVELOPER_2019
)
echo Installing new instance [%NEW_SQL_INSTANCE_NAME%] of [%KEY%]

echo DOWNLOADING SQL %v% BOOTSTRAPPER
set url=https://download.microsoft.com/download/2/4/2/242f3dff-d1c1-40d7-8e0f-19f43842dfaf/SQL2019-SSEI-Eval.exe
set url=https://download.microsoft.com/download/b/8/c/b8ce1000-2e0b-4bc8-b4e4-646e9a439525/SQL2019-SSEI-Dev.exe
set url=https://download.microsoft.com/download/d/a/2/da259851-b941-459d-989c-54a18a5d44dd/SQL2019-SSEI-Dev.exe
set outfile=%AppData%\Temp\%KEY%.exe
mkdir "%AppData%\Temp" 1>nul 2>&1
echo [System.Net.ServicePointManager]::ServerCertificateValidationCallback={$true}; $d=new-object System.Net.WebClient; $d.DownloadFile("$Env:url","$Env:outfile") | powershell -command -

echo DOWNLOADING SQL %v%
echo Y | "%outfile%" /ENU /Q /Action=Download /MEDIATYPE=CAB /MEDIAPATH="%AppData%\Temp\%KEY%"
set file=SQLEXPR_x64_ENU.exe
"%AppData%\Temp\%KEY%\SQLServer2019-DEV-x64-ENU.exe" /qs /x:"%AppData%\Temp\%KEY%\extracted"
del /q "%AppData%\Temp\%KEY%\SQLServer2019-x64-ENU.exe" >nul 2>&1

rem The supported features on Windows Server Core are: 
rem   Database Engine Services, 
rem   SQL Server Replication, 
rem   Full-Text and Semantic Extractions for Search, 
rem   Analysis Services, 
rem   Client Tools Connectivity, 
rem   Integration Services, 
rem   and SQL Client Connectivity SDK.

for /f "delims=;" %%i in ('powershell -command "(Get-WmiObject -Class Win32_Group | where { $_.SID -eq \"S-1-5-32-545\" }).Name"') DO set users=%%i
If Not Defined users Set users=Users

"%AppData%\Temp\%KEY%\extracted\Setup.exe" /QUIETSIMPLE /ENU /INDICATEPROGRESS /ACTION=Install ^
  /IAcceptSQLServerLicenseTerms /IACCEPTROPENLICENSETERMS ^
  /UpdateEnabled=True ^
  /FEATURES=SQLENGINE,REPLICATION,FullText ^
  /INSTANCENAME="%NEW_SQL_INSTANCE_NAME%" ^
  /INSTANCEDIR="%SystemDrive%\SQL" ^
  /INSTALLSHAREDDIR="%SystemDrive%\SQL\x64" ^
  /INSTALLSHAREDWOWDIR="%SystemDrive%\SQL\x86" ^
  /SECURITYMODE="SQL" ^
  /SAPWD="`1qazxsw2" ^
  /SQLSVCACCOUNT="NT AUTHORITY\SYSTEM" ^
  /SQLSVCSTARTUPTYPE=AUTOMATIC ^
  /BROWSERSVCSTARTUPTYPE=AUTOMATIC ^
  /ADDCURRENTUSERASSQLADMIN=False ^
  /SQLSYSADMINACCOUNTS="BUILTIN\%USERS%" ^
  /TCPENABLED=1 /NPENABLED=1

rd /q /s "%AppData%\Temp\%KEY%"
