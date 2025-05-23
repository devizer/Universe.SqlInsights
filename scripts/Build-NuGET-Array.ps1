$ErrorActionPreference="Stop"
Say "Building NuGet Packages. Cpu is '$(Get-Cpu-Name)'. $((Get-Memory-Info).Description)"

& dotnet --list-sdks | Sort-Object -Descending
. .\Includes.ps1
$NUnit_Pipeline_Revision=212
$This_SqlIsnights_Version_Base="0.9.0"

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

$sed="C:\Apps\Git\usr\bin\sed.exe"; if (-Not (test-Path $sed)) { $sed="sed"; }
Write-Host "sed is [$sed]"


# $nunit_versions = @("3.13.2", "4.2.2");
$projects = @("Universe.NUnitPipeline.SqlServerDatabaseFactory", "Universe.SqlInsights.NUnit");

$Commit_Count = Get-Commit-Count
$This_SqlIsnights_Version = "$This_SqlIsnights_Version_Base.$Commit_Count"
write-host "Commit Count: $Commit_Count"

$Work_Base="$ENV:SQLINSIGHTS_NUGET_BUILDER_FOLDER"
if (-not $Work_Base) { $Work_Base="W:\Build\Universe.SqlInsights"; }
Remove-Item -Recurse -Force "$Work_Base" -EA SilentlyContinue | Out-Null
$DS="$([System.IO.Path]::DirectorySeparatorChar)"
$logFolder="$Work_Base$($DS)BUILD-LOGS"
New-Item $logFolder -Force -ItemType Container -EA SilentlyContinue | Out-Null
& git clone https://github.com:/devizer/Universe.SqlInsights "$($Work_Base)$($DS)Source"
# Remove-Item -Recurse -Force "$Work_Base\Source\.git" -EA SilentlyContinue | Out-Null

Say "REMOVING net framework projects"
pushd "$Work_Base\Source\src"
& dotnet sln Universe.SqlInsights.sln remove AdventureWorks$($DS)AdventureWorks.csproj
& dotnet sln Universe.SqlInsights.sln remove AdventureWorks.HeadlessTests$($DS)AdventureWorks.HeadlessTests.csproj
& dotnet sln Universe.SqlInsights.sln remove AdventureWorks.Tests$($DS)AdventureWorks.Tests.csproj
& dotnet sln Universe.SqlInsights.sln remove Universe.SqlInsights.SqlServerStorage.Tests$($DS)Universe.SqlInsights.SqlServerStorage.Tests.csproj
& dotnet sln Universe.SqlInsights.sln remove Universe.SqlInsights.W3Api.Client.Tests$($DS)Universe.SqlInsights.W3Api.Client.Tests.csproj
Say "PARALLEL RESTORE"
& dotnet restore Universe.SqlInsights.sln -v:q
popd

Say "PATCHING projects as version $This_SqlIsnights_Version"
$csprojs = @(Get-ChildItem -Path "$Work_Base" -Filter "*.csproj" -Recurse)
foreach($csproj in $csprojs) {
  # Write-Host "Patch $($csproj.FullName) as version $($This_SqlIsnights_Version)"
  Set-CS-Project-Version "$($csproj.FullName)" "$This_SqlIsnights_Version"
}


$buildIndex = 0;
$buildCount = $projects.Length * $nunit_versions.Count
foreach($NUnit_Version in $nunit_versions) {
  $work="$Work_Base$($DS)$NUnit_Version"
  New-Item "$work" -Force -ItemType Container -EA SilentlyContinue | Out-Null
  Copy-Item "$Work_Base$($DS)Source" -Filter *.* -Destination "$work" -Recurse | out-null
  pushd "$work$($DS)Source"
  foreach($project in $projects) {
    $buildIndex++;
    Write-Host "";
    Say "BUILDING $BuildIndex of $($buildCount): $nunit_Version $project"
    $This_NUnit_Version = "$NUnit_Version.$Commit_Count"
    write-host "THIS VERSION: $This_NUnit_Version"

    Set-Target-Version-for-NUnit-Version $NUnit_Version
    Write-Host "TARGET_FRAMEWORKS_LIB:  $($TARGET_FRAMEWORKS_LIB)" -ForegroundColor Magenta
    Write-Host "TARGET_FRAMEWORKS_TEST: $($TARGET_FRAMEWORKS_TEST)" -ForegroundColor Magenta

    $currentNUnitPipelineVersion = "$NUnit_Version.$NUnit_Pipeline_Revision"
    pushd "src$($DS)$project"
    & "$sed" @("-i", "-E", "s|<TargetFrameworks>.*</TargetFrameworks>|<TargetFrameworks>$TARGET_FRAMEWORKS_LIB</TargetFrameworks>|", "$($project).csproj")
    Write-Host "REFERENCE Universe.NUnitPipeline VERSION [$nunit_Version.$NUnit_Pipeline_Revision]"
    & dotnet remove package Universe.NUnitPipeline
    & dotnet add package Universe.NUnitPipeline -v "$nunit_Version.$NUnit_Pipeline_Revision" --no-restore
    Set-CS-Project-Version "$PWD$($DS)$($project).csproj" "$This_NUnit_Version"
    Try-And-Retry "Build $project $This_NUnit_Version" { & { dotnet @("build", "-c", "Release") 2>&1 } *| tee "$logFolder$($DS)$nunit_Version-$($project)-build.log" }
    if ($nunit_Version -eq $Full_NUnit_Version) {
      cd ..
      & { dotnet @("build", "-c", "Release") 2>&1 } *| tee "$logFolder$($DS)ALL-build.log" 
    }
    popd
    Write-Host ""
  } 
  popd 
}

Say "Finish. Cpu is '$(Get-Cpu-Name)'. $((Get-Memory-Info).Description)"
foreach($ext in @("nupkg", "snupkg")) {
  $nupkgs = @(Get-ChildItem -Path "$Work_Base" -Filter "*.$ext" -Recurse)
  Write-Host "Copying $($nupkgs.Length) (s)nupkg-files"
  $nupkgs | Copy-Item -Destination "$Work_Base$($DS)"
}

# Logs
& 7z a "$Work_Base$($DS)BUILD-LOGS.7z" "$logFolder$($DS)"

Write-Host "Reminder: Do NOT publish [Universe.SqlInsights.W3Api.Client.*.nupkg]" -ForegroundColor Yellow
