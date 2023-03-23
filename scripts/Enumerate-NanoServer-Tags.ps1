$allTags=(Invoke-WebRequest https://mcr.microsoft.com/v2/windows/nanoserver/tags/list | ConvertFrom-Json).tags
$tags=$allTags | Where { -not ($_ -like "*-*" -or $_ -like "*_*") }
$tags=$tags | sort -Descending


New-Item -ItemType Directory -Path "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Nano Server Manifests" -Force -EA SilentlyContinue | Out-Null
$n=0;
foreach($tag in $tags) {
  $n++
  if ($tag -like "10.*") { 
    $ver=$tag
  } else {
    $output=(& docker.exe manifest inspect "mcr.microsoft.com/windows/nanoserver:$tag" | Out-String)
    echo $output > "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Nano Server Manifests\$tag.txt"
    Write-Host "OUTPUT for $tag"; Write-Host $output
    $json=($output | ConvertFrom-Json)
    # $count=$json.manifests.Length
    # $platform=$json.manifests[0].platform
    # $ver=$platform | Get-Member -Name "os.version" -MemberType Property
    $ver=$json.manifests[0].platform."os.version"
  }
  # $ver=$json.manifests[0].platform."os.version"
  Write-Host "$n of $($tags.Length) '$($tag)': os.version=$ver"
  echo "$($tag): $ver" | tee "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Nano Server Versions.txt" -Append
}
