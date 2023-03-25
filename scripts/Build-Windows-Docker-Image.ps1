function Get-Elapsed
{
    if ($Global:startAt -eq $null) { $Global:startAt = [System.Diagnostics.Stopwatch]::StartNew(); }
    [System.String]::Concat("[", (new-object System.DateTime(0)).AddMilliseconds($Global:startAt.ElapsedMilliseconds).ToString("mm:ss"), "]");
}; Get-Elapsed | out-null;

function Say { param( [string] $message )
    # Write-Host "$(Get-Elapsed) " -NoNewline -ForegroundColor Magenta
    # Write-Host "$message" -ForegroundColor Yellow
    [System.Console]::Write("[35m" + "$(Get-Elapsed) " + "[0m")
    [System.Console]::WriteLine("$message ")
}


function Delete-Docker-Hub-Tag() {
  param([Hashtable] $Options, [string] $Tag)
  if (-not $Options.Token) {
      # https://devopscell.com/docker/dockerhub/2018/04/09/delete-docker-image-tag-dockerhub.html
      $login_data=@{ username=$Options.User; password=$Options.Password }
      try {
        $tokenResponse = Invoke-WebRequest -Uri "https://hub.docker.com/v2/users/login/" -Body "$($login_data | ConvertTo-Json)" -Method POST -ContentType "application/json"
        $token=($tokenResponse | ConvertFrom-Json).token
        $Options | Add-Member -NotePropertyName Token -NotePropertyValue "$token"
        Write-Host "SUCCESSFUL TOKEN"
      } catch {
        $statusCode = "$($_.Exception.Status)"
        Write-Host "DOCKER HUB AUTH FAILED"
      }
      if ($statusCode -eq "ProtocolError") { $about="Unauthorized"; }
  }

  try {
    $result = Invoke-WebRequest -Headers @{Authorization="JWT $($Options.Token)"} -Uri "https://hub.docker.com/v2/repositories/$($Options.Org)/$($Options.Repo)/tags/$TAG/" -Method DELETE
    Write-Host "SUCCESSFUL DELETED TAG '$Tag'"
    return $true
  } catch {
    $result = $_.Exception.Response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($result)
    $reader.BaseStream.Position = 0
    $reader.DiscardBufferedData()
    $responseBody = $reader.ReadToEnd();
    $responseBody = $responseBody.TrimEnd(@([char]13,[char]10))
    Write-Host "DELETE TAG ERROR: $responseBody"
    return $false
  }
  return $result
}

# $options=@{ Org="devizervlad"; Repo="sqlinsights-dashboard-nanoserver"; User="devizervlad"; Password=$ENV:PASSWRD2; }
# Delete-Docker-Hub-Tag $options "Tag1"



$nanoVersions = $(
  @{  TAG = "ltsc2022"; Version = "10.0.20348.1607" },
  @{  TAG = "20H2";     Version = "10.0.19042.1889" },
  @{  TAG = "2004";     Version = "10.0.19041.1415" },
  @{  TAG = "1909";     Version = "10.0.18363.1556" },
  @{  TAG = "1903";     Version = "10.0.18362.1256" },
  @{  TAG = "ltsc2019"; Version = "10.0.17763.4131" }, # 1809
# @{  TAG = "1809";     Version = "10.0.17763.4131" }, # LTS 2019
  @{  TAG = "1803";     Version = "10.0.17134.1305" },
  @{  TAG = "1709";     Version = "10.0.16299.1087" },
  @{  TAG = "sac2016";  Version = "10.0.14393.2068" }  # 1607
  
)

pushd Windows-W3API-Docker

$version=$ENV:SQLINSIGHTS_VERSION
$image="devizervlad/sqlinsights-dashboard-nanoserver"

Say "Building :latest-on-1607"
docker build --build-arg TAG=sac2016 -t "$($image):latest-on-1607" .
docker push -q "$($image):latest-on-1607"
docker manifest inspect "$($image):latest-on-1607"

$n=0
foreach($nanoVersion in $nanoVersions) {
  $n++
  $tag=$nanoVersion.Tag;
  $ver=$nanoVersion.Version
  $imageTag="$($version)-$($tag)"
  echo "";
  Say "$n of $($nanoVersions.Length). BUILDING TAG '$tag', VERSION $ver"
  docker pull -q "mcr.microsoft.com/windows/nanoserver:$ver"
  & docker build --build-arg TAG=$tag -t "$($image):$($imageTag)" .
  Say "PUSH $($image):$($imageTag), mandatory"
  & docker push -q "$($image):$($imageTag)"
}
echo "________________________________________________________________________________"

# TODO: v2.5.714 is multiarch, but v2.5.715 IS NOT. What the hell
# https://docs.docker.com/engine/reference/commandline/manifest/
# https://github.com/docker/hub-tool


foreach($tagVer in @($version, "latest")) {

  $manifestCreateParams = "$($image):$($tagVer)"
  foreach($nanoVersion in $nanoVersions) {
    $tag=$nanoVersion.Tag;
    $ver=$nanoVersion.Version
    $imageTag="$($version)-$($tag)"
    $manifestCreateParams += " --amend $($image):$($imageTag)"
  }

  Say "CREATE MANIFEST ARGUMENTS for '$tagVer': [$manifestCreateParams]"
  & cmd.exe /c "docker manifest create $manifestCreateParams"

  Say "1ST INTERMEDIATE INSPECT MANIFEST for '$tagVer'"
  & docker manifest inspect "$($image):$($tagVer)"

  Say "DOCKER MANIFEST PUSH for '$tagVer'"
  & docker manifest push "$($image):$($tagVer)"
}

# Delete Intermediate Tags
$options=@{ Org="devizervlad"; Repo="sqlinsights-dashboard-nanoserver"; User="devizervlad"; Password=$ENV:PASSWORD1; }
foreach($nanoVersion in $nanoVersions) {
  $tag=$nanoVersion.Tag;
  $ver=$nanoVersion.Version
  $imageTag="$($version)-$($tag)"
  Say "DELETE INTERMEDIATE TAG [$imageTag]"
  Delete-Docker-Hub-Tag $options "$imageTag"
}

<# 
  BAD IDEA: It breaks multiarch
  Say "Pull [$($image):$($version)]"
  docker pull "$($image):$($version)"

  Say "TAG '$($version)' AS 'latest'"
  & docker tag "$($image):$($version)" "$($image):latest"

  Say "PUSH ALL TAGS FOR '$($image)'"
  & docker push --all-tags "$($image)"

  Say "PUSH '$version' FOR '$($image)'"
  & docker push "$($image):$version"

  Say "PUSH 'latest' FOR '$($image)'"
  & docker push "$($image):latest"

#>


Say "DOCKER IMAGES"
docker images

<#
foreach($nanoVersion in $nanoVersions) {
  $tag=$nanoVersion.Tag;
  $ver=$nanoVersion.Version
  $imageTag="$($version)-$($tag)"
  Say "Annotate version OS VERSION '$ver' for tag '$tag'"
  & docker manifest annotate --arch amd64 --os windows --os-version $ver "$($image):$($version)" "$($image):$($imageTag)" 
}

Say "2nd Final Inspect Manifest"
& docker manifest inspect "$($image):$($version)"
#>

Say "DONE"
popd
