
$nanoVersions = $(
  @{  TAG = "ltsc2022";
      Version = "10.10.10.10"
   }
)

pushd Windows-W3API-Docker
$tag="ltsc2022"
& docker build --build-arg TAG=$tag -t devizervlad/sqlinsights-dashboard-nanoserver:$tag .
popd