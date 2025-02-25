$revision=203
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
"4.2.2"
)

# $nunit_versions = @("3.13.2", "4.2.2");
$projects = @("Universe.NUnitPipeline.SqlServerDatabaseFactory", "Universe.SqlInsights.NUnit");

function Get-Commit-Count() {
  $commitsRaw = & { set TZ=GMT; git log -999999 --date=raw --pretty=format:"%cd" }
  $lines = $commitsRaw.Split([Environment]::NewLine)
  $commitCount = $lines.Length
  return $commitCount
}
function Get-Elapsed { 
    if ($Global:_Say_Stopwatch -eq $null) { $Global:_Say_Stopwatch = [System.Diagnostics.Stopwatch]::StartNew(); }
    $milliSeconds=$Global:_Say_Stopwatch.ElapsedMilliseconds
    if ($milliSeconds -ge 3600000) { $format="HH:mm:ss"; } else { $format="mm:ss"; }
    return "[$((new-object System.DateTime(0)).AddMilliseconds($milliSeconds).ToString($format))]"
}; $Global:_Say_Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

function Set-Target-Version-for-NUnit-Version([string] $NUnit_Version) {
  if ($NUnit_Version -like "3.7.*" -or $NUnit_Version -like "3.8.*" -or $NUnit_Version -like "3.9.*") {
      # the latest net20
      $forced="netstandard2.0;net462;net46"
      $GLOBAL:TARGET_FRAMEWORKS_LIB="netstandard1.3;netstandard1.6;net35;net40;net45;$forced"
      $GLOBAL:TARGET_FRAMEWORKS_TEST="netcoreapp1.0;netcoreapp1.1;netcoreapp2.0;netcoreapp2.1;netcoreapp3.0;netcoreapp3.1;net20;net35;net40;net45;net462;net48;net8.0"
  }
  elseif ($NUnit_Version -like "3.10.*") {
      # the latest net20
      $forced="net462;net46"
      $GLOBAL:TARGET_FRAMEWORKS_LIB="netstandard1.6;netstandard2.0;net35;net4.0;net45;$forced"
      $GLOBAL:TARGET_FRAMEWORKS_TEST="netcoreapp1.0;netcoreapp1.1;netcoreapp2.0;netcoreapp2.1;netcoreapp3.0;netcoreapp3.1;net20;net35;net40;net45;net462;net48;net8.0"
  }
  elseif ($NUnit_Version -like "3.*") {
      $forced="net46;net462"
      $GLOBAL:TARGET_FRAMEWORKS_LIB="netstandard2.0;net35;net40;net45;$forced"
      $GLOBAL:TARGET_FRAMEWORKS_TEST="net8.0;net6.0;netcoreapp3.1;net48;net462;net45;net40;net35"
  }
  else {
      # v4.x
      $GLOBAL:TARGET_FRAMEWORKS_LIB="net462;net6.0";
      $GLOBAL:TARGET_FRAMEWORKS_TEST="net8.0;net6.0;net48;net462";
  }
}

$Commit_Count = Get-Commit-Count
write-host "Commit Count: $Commit_Count"

$Work_Base="W:\Temp\Universe.SqlInsights-NUnit-Packages"
Remove-Item -Recurse -Force "$Work_Base" -EA SilentlyContinue | Out-Null
New-Item "$Work_Base" -Force -ItemType Container -EA SilentlyContinue | Out-Null
& git.exe clone https://github.com:/devizer/Universe.SqlInsights "$Work_Base\base"
# Remove-Item -Recurse -Force "$Work_Base\base\.git" -EA SilentlyContinue | Out-Null


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
    $This_Version = "$NUnit_Version.$Commit_Count"
    write-host "THIS VERSION: $this_version"

    Set-Target-Version-for-NUnit-Version $NUnit_Version
    Write-Host "TARGET_FRAMEWORKS_LIB:  $($TARGET_FRAMEWORKS_LIB)" -ForegroundColor Magenta
    Write-Host "TARGET_FRAMEWORKS_TEST: $($TARGET_FRAMEWORKS_TEST)" -ForegroundColor Magenta

    pushd src\$project
    & C:\Apps\Git\usr\bin\sed.exe "-i", "-E", "s|<TargetFrameworks>.*</TargetFrameworks>|<TargetFrameworks>$TARGET_FRAMEWORKS_LIB</TargetFrameworks>|" "$($project).csproj"
    & dotnet remove package Universe.NUnitPipeline
    & dotnet add package Universe.NUnitPipeline -v "$nunit_Version.$revision"
    & { dotnet "build", "-c", "Release", "-p:PackageVersion=$This_Version", "-p:Version=$This_Version" } *| tee "..\..\..\$nunit_Version-$($project)-build.log"
    popd
    Write-Host ""
  } 
  popd 
}

Write-Host "$(Get-Elapsed) Finish" -ForegroundColor Magenta
$nupkgs = @(Get-ChildItem -Path "$Work_Base" -Filter "*.nupkg" -Recurse)
Write-Host "Copying $($nupkgs.Length) nupkg-files"
$nupkgs | Copy-Item -Destination "$Work_Base\"
