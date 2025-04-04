. .\Includes.ps1
$NUnit_Pipeline_Revision=212
$This_SqlIsnights_Version_Base="0.4.4"
$nunit_versions = @(
  "3.7.0",
  "3.7.1",
  "3.8.0",
  "3.8.1",
  "3.9.0",
  "3.10.0",
  "3.10.1",
  "3.12.0",
  "3.13.0",
  "3.13.1",
  "3.13.2",
  "3.13.3",
  "3.14.0",
  "4.0.1",
  "4.1.0"
  "4.2.1",
  "4.2.2",
  "4.3.0",
  "4.3.1",
  "4.3.2"
)
$Full_NUnit_Version = "3.14.0"



# $nunit_versions = @("3.13.2", "4.2.2");
$projects = @("Universe.NUnitPipeline.SqlServerDatabaseFactory", "Universe.SqlInsights.NUnit");

$Commit_Count = Get-Commit-Count
$This_SqlIsnights_Version = "$This_SqlIsnights_Version_Base.$Commit_Count"
write-host "Commit Count: $Commit_Count"

$Work_Base="W:\Build\Universe.SqlInsights"
Remove-Item -Recurse -Force "$Work_Base" -EA SilentlyContinue | Out-Null
New-Item "$Work_Base" -Force -ItemType Container -EA SilentlyContinue | Out-Null
& git.exe clone https://github.com:/devizer/Universe.SqlInsights "$Work_Base\base"
# Remove-Item -Recurse -Force "$Work_Base\base\.git" -EA SilentlyContinue | Out-Null

$csprojs = @(Get-ChildItem -Path "$Work_Base" -Filter "*.csproj" -Recurse)
foreach($csproj in $csprojs) {
  Write-Host "Patch $($csproj.FullName) as version $($This_SqlIsnights_Version)"
  Set-CS-Project-Version "$($csproj.FullName)" "$This_SqlIsnights_Version"
}


$buildIndex = 0;
$buildCount = $projects.Length * $nunit_versions.Count
foreach($NUnit_Version in $nunit_versions) {
  $work="$Work_Base\$NUnit_Version"
  New-Item "$work" -Force -ItemType Container -EA SilentlyContinue | Out-Null
  Copy-Item "$Work_Base\base" -Filter *.* -Destination "$work" -Recurse | out-null
  pushd "$work\base"
  foreach($project in $projects) {
    $buildIndex++;
    Write-Host "$(Get-Elapsed) $BuildIndex of $($buildCount): $nunit_Version $project" -ForegroundColor Magenta
    $This_NUnit_Version = "$NUnit_Version.$Commit_Count"
    write-host "THIS VERSION: $This_NUnit_Version"

    Set-Target-Version-for-NUnit-Version $NUnit_Version
    Write-Host "TARGET_FRAMEWORKS_LIB:  $($TARGET_FRAMEWORKS_LIB)" -ForegroundColor Magenta
    Write-Host "TARGET_FRAMEWORKS_TEST: $($TARGET_FRAMEWORKS_TEST)" -ForegroundColor Magenta

    pushd src\$project
    & C:\Apps\Git\usr\bin\sed.exe "-i", "-E", "s|<TargetFrameworks>.*</TargetFrameworks>|<TargetFrameworks>$TARGET_FRAMEWORKS_LIB</TargetFrameworks>|" "$($project).csproj"
    & dotnet remove package Universe.NUnitPipeline
    & dotnet add package Universe.NUnitPipeline -v "$nunit_Version.$NUnit_Pipeline_Revision"
    Set-CS-Project-Version "$PWD\$($project).csproj" "$This_NUnit_Version"
    & { dotnet "build", "-c", "Release" 2>&1 } *| tee "..\..\..\$nunit_Version-$($project)-build.log"
    if ($nunit_Version -eq $Full_NUnit_Version) {
      cd ..
      & dotnet build "-c" Release
    }
    popd
    Write-Host ""
  } 
  popd 
}

Write-Host "$(Get-Elapsed) Finish" -ForegroundColor Magenta
$nupkgs = @(Get-ChildItem -Path "$Work_Base" -Filter "*.nupkg" -Recurse)
Write-Host "Copying $($nupkgs.Length) nupkg-files"
$nupkgs | Copy-Item -Destination "$Work_Base\"

Write-Host "Remidned: Do NOT publish [Universe.SqlInsights.W3Api.Client.*.nupkg]" -ForegroundColor Yellow


