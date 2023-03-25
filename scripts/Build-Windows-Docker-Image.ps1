# https://learn.microsoft.com/en-us/virtualization/windowscontainers/deploy-containers/version-compatibility

function Say { <# param( [string] $message ) #>
    if ($Global:startSayAt -eq $null) { $Global:startSayAt = [System.Diagnostics.Stopwatch]::StartNew(); }
    $fmt=$Global:SayTimeFormat;
    if ("$fmt" -eq "") { $fmt = "mm:ss"; }
    $elapsed = "[" + (new-object System.DateTime(0)).AddMilliseconds($Global:startSayAt.ElapsedMilliseconds).ToString($fmt) + "]";

    # https://ss64.com/nt/echoansi.txt
    # https://ss64.com/nt/syntax-ansi.html
    $fullMessage="$Args"
    $hasAnsi = $false
    if ([System.Environment]::OSVersion.Platform -like "Win*") {
      [System.Version] $ver = [System.Environment]::OSVersion.Version;
      $ver1909 = new-object "System.Version"(10, 0, 18363);
      $ver1511 = new-object "System.Version"(10, 0, 10586);
      if ($ver -ge $ver1909) { $hasAnsi = $true; }
      elseif ($ver -ge $ver1511) {
        $hasAnsi = $true;
        New-ItemProperty -Path "HKCU:\Console" -Name "VirtualTerminalLevel" -Value 1 -PropertyType DWORD -Force;
      }
    }
    if ($hasAnsi) {
      $e="$([char]27)"
      [System.Console]::Write("$e[94m$e[1m" + "$($elapsed) " + "$e[0m")
      [System.Console]::WriteLine("$e[33m$e[1m" + "$fullMessage" + "$e[0m")
    } else {
      Write-Host "$($elapsed) " -NoNewline -ForegroundColor Magenta
      Write-Host "$fullMessage" -ForegroundColor Yellow
  }
}; $Global:startSayAt = [System.Diagnostics.Stopwatch]::StartNew();


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
