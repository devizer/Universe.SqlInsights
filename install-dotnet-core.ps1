& taskkill /T /F /IM dotnet.exe
pushd "$($Env:UserProfile)"
Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile 'dotnet-install.ps1';

$targetDir = "$($Env:ProgramFiles)\dotnet"; if (-not $targetDir) { "C:\Program Files\dotnet"; }
# $targetDir = "V:\Temp\DotNet"
# foreach ($v in @("1.0", "1.1", "2.0", "2.1", "2.2", "3.0", "3.1", "5.0", "6.0")) {
$versions = @("2.1", "2.2", "3.0", "3.1", "5.0", "6.0", "7.0")
$versions = @("6.0")
foreach ($v in @($versions)) {
  Write-Host "Installing .NET Core $v to [$targetDir]" -ForegroundColor Magenta
  ./dotnet-install.ps1 -InstallDir "$($targetDir)" -Channel $v;
}

popd
exit 0;