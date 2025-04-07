function Set-CS-Project-Version([string] $csproj, [string]$version) {
  $raw = Get-Content -Raw $csproj
  if ($raw.IndexOf("http://schemas.microsoft.com/developer/msbuild/2003") -ge 0) { return; }
  $lines = @(Get-Content $csproj)
  $lines = @($lines | ? { -not ($_ -match "<PackageVersion>" -or $_ -match "<Version>") })
  $newLine = "<!-- Auto Generated --> <PropertyGroup><PackageVersion>$version</PackageVersion><Version>$version</Version></PropertyGroup> <!-- Auto Generated -->"
  $newLines = @($lines[0]) + @($newLine) + @($lines[1..($lines.Count-1)])
  Write-All-Text "$csproj" ($newLines -join "`r`n")
  # Write-Host "[$($csproj)] at [$PWD]`r`n $($newLines -join "`r`n")"
}

function Create-Directory($dirName) { 
  if ($dirName) { 
    $err = $null;
    try { 
      $_ = [System.IO.Directory]::CreateDirectory($dirName); return; 
    } catch {
      $err = "Create-Directory failed for `"$dirName`". $($_.Exception.GetType().ToString()) $($_.Exception.Message)"
      Write-Host "Warning! $err";
      throw $err;
    }
  }
}

function Create-Directory-for-File($fileFullName) { 
  $dirName=[System.IO.Path]::GetDirectoryName($fileFullName)
  Create-Directory "$dirName";
}

function Write-All-Text( [string]$file, [string]$text ) {
  Create-Directory-for-File $file
  $utf8=new-object System.Text.UTF8Encoding($false); 
  [System.IO.File]::WriteAllText($file, $text, $utf8);
}
function Get-Commit-Count() {
  $commitsRaw = & { set TZ=GMT; git log -999999 --date=raw --pretty=format:"%cd" }
  $lines = $commitsRaw.Split([Environment]::NewLine)
  $commitCount = $lines.Length
  return $commitCount
}
function Get-Elapsed { 
    if ($Global:_Say_Stopwatch -eq $null) { $Global:_Say_Stopwatch = [System.Diagnostics.Stopwatch]::StartNew(); }
    $milliSeconds=$Global:_Say_Stopwatch.ElapsedMilliseconds
    if ($milliSeconds -ge 3599500) { $format="HH:mm:ss"; } else { $format="mm:ss"; }
    return "[$((new-object System.DateTime(0)).AddMilliseconds($milliSeconds).ToString($format))]"
}; $Global:_Say_Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

function Set-Target-Version-for-NUnit-Version([string] $NUnit_Version) {
  if ($NUnit_Version -like "3.7.*" -or $NUnit_Version -like "3.8.*" -or $NUnit_Version -like "3.9.*") {
      # the latest net20
      $forced="netstandard2.0;net462;net46;net48;net6.0;net8.0"
      $GLOBAL:TARGET_FRAMEWORKS_LIB="netstandard1.3;netstandard1.6;net35;net40;net45;$forced"
      $GLOBAL:TARGET_FRAMEWORKS_TEST="netcoreapp1.0;netcoreapp1.1;netcoreapp2.0;netcoreapp2.1;netcoreapp3.0;netcoreapp3.1;net20;net35;net40;net45;net462;net48;net8.0"
  }
  elseif ($NUnit_Version -like "3.10.*") {
      # the latest net20
      $forced="net462;net46;net48;net6.0;net8.0"
      $GLOBAL:TARGET_FRAMEWORKS_LIB="netstandard1.6;netstandard2.0;net35;net40;net45;$forced"
      $GLOBAL:TARGET_FRAMEWORKS_TEST="netcoreapp1.0;netcoreapp1.1;netcoreapp2.0;netcoreapp2.1;netcoreapp3.0;netcoreapp3.1;net20;net35;net40;net45;net462;net48;net8.0"
  }
  elseif ($NUnit_Version -like "3.*") {
      $forced="net46;net462;net48;net6.0;net8.0"
      $GLOBAL:TARGET_FRAMEWORKS_LIB="netstandard2.0;net35;net40;net45;$forced"
      $GLOBAL:TARGET_FRAMEWORKS_TEST="net8.0;net6.0;netcoreapp3.1;net48;net462;net45;net40;net35"
  }
  else {
      # v4.x
      $forced="net46;net462;net48;net6.0;net8.0"
      $GLOBAL:TARGET_FRAMEWORKS_LIB="net462;net6.0;net8.0";
      $GLOBAL:TARGET_FRAMEWORKS_TEST="net8.0;net6.0;net48;net462";
  }
}
