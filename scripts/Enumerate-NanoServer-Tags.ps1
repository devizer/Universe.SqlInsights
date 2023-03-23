$allTags=(Invoke-WebRequest https://mcr.microsoft.com/v2/windows/nanoserver/tags/list | ConvertFrom-Json).tags
$tags=$allTags | Where { -not ($_ -like "*-*" -or $_ -like "*_*") }

$n=0;
foreach($tag in $tags) {
  $n++
  $json=(& docker.exe manifest inspect "mcr.microsoft.com/windows/nanoserver:$tag" | ConvertTo-Json)
  $count=$json.manifests.Length
  $ver=$json.platform | Get-Member -Name "os.version" -MemberType Property
  Write-Host "$n of $($tags.Length) '$($tag)': manifests count = $count, os.version=$ver"
  echo "$(tag): $ver" | tee "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Nano Server Versions.txt" -Append
}
