function Get-Elapsed
{
    if ($Global:startAt -eq $null) { $Global:startAt = [System.Diagnostics.Stopwatch]::StartNew(); }
    [System.String]::Concat("[", (new-object System.DateTime(0)).AddMilliseconds($Global:startAt.ElapsedMilliseconds).ToString("mm:ss"), "]");
}; Get-Elapsed | out-null;

function Say { param( [string] $message )
    Write-Host "$(Get-Elapsed) " -NoNewline -ForegroundColor Magenta
    Write-Host "$message" -ForegroundColor Yellow
}


$nanoVersions = $(
  @{  TAG = "ltsc2022"; Version = "10.0.20348.1607" },
  @{  TAG = "20H2";     Version = "10.0.19042.1889" },
  @{  TAG = "2004";     Version = "10.0.19041.1415" },
  @{  TAG = "1909";     Version = "10.0.18363.1556" },
  @{  TAG = "1903";     Version = "10.0.18362.1256" },
  @{  TAG = "ltsc2019"; Version = "10.0.17763.4131" }, # 1809
# @{  TAG = "1809";     Version = "10.0.17763.4131" }, # LTS 2019
  @{  TAG = "1803";     Version = "10.0.17134.1305" },
  @{  TAG = "1709";     Version = "10.0.16299.1087" }
)

$version=$ENV:SQLINSIGHTS_VERSION
$image="devizervlad/sqlinsights-dashboard-nanoserver"
pushd Windows-W3API-Docker
foreach($nanoVersion in $nanoVersions) {
  $tag=$nanoVersion.Tag;
  $ver=$nanoVersion.Version
  $imageTag="$($version)-$($tag)"
  echo "";
  Say "Building Tag '$tag', version $ver"
  docker pull -q "$($image):$($imageTag)"
  & docker build --build-arg TAG=$tag -t "$($image):$($imageTag)" .
  Say "Push $($image):$($imageTag)"
  & docker push "$($image):$($imageTag)"
}

$manifestCreateParams = "$($image):$($version)"
foreach($nanoVersion in $nanoVersions) {
  $tag=$nanoVersion.Tag;
  $ver=$nanoVersion.Version
  $imageTag="$($version)-$($tag)"
  $manifestCreateParams += " --amend $($image):$($imageTag)"
}

Say "Create Manifest Args: [$manifestCreateParams]"
& cmd.exe /c "docker manifest create $manifestCreateParams"

Say "1st Intermediate Inspect Manifest"
& docker manifest inspect "$($image):$($version)"

foreach($nanoVersion in $nanoVersions) {
  $tag=$nanoVersion.Tag;
  $ver=$nanoVersion.Version
  $imageTag="$($version)-$($tag)"
  Say "Annotate version OS VERSION '$ver' for tag '$tag'"
  & docker manifest annotate --arch amd64 --os windows --os-version $ver "$($image):$($version)" "$($image):$($imageTag)" 
}

Say "2nd Final Inspect Manifest"
& docker manifest inspect "$($image):$($version)"

Say "DONE"
popd
