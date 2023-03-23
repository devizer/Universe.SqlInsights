$allTags=(Invoke-WebRequest https://mcr.microsoft.com/v2/windows/nanoserver/tags/list | ConvertFrom-Json).tags
$tags=$allTags | Where { -not ($_ -like "*-*" -or $_ -like "*_*") }
$tags=$tags | sort -Descending


New-Item -ItemType Directory -Path "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Nano Server Manifests" -Force -EA SilentlyContinue | Out-Null
$n=0;
foreach($tag in $tags) {
  $n++
  $output=(& docker.exe manifest inspect "mcr.microsoft.com/windows/nanoserver:$tag" | Out-String)
  echo $output > "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Nano Server Manifests\$tag.txt"
  Write-Host "OUTPUT for $tag"; Write-Host $output
  $json=($output | ConvertTo-Json)
  $json=(& docker.exe manifest inspect "mcr.microsoft.com/windows/nanoserver:$tag" | ConvertTo-Json)
  $count=$json.manifests.Length
  $ver=$json.manifests[0].platform | Get-Member -Name "os.version" -MemberType Property
  Write-Host "$n of $($tags.Length) '$($tag)': manifests count = $count, os.version=$ver"
  echo "$($tag): $ver" | tee "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Nano Server Versions.txt" -Append
}
