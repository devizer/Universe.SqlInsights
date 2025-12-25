# export NODE_VER=v16.20.2; script=https://raw.githubusercontent.com/devizer/glist/master/install-dotnet-and-nodejs.sh; (wget -q -nv --no-check-certificate -O - $script 2>/dev/null || curl -ksSL $script) | bash -s node
BASE=/var/lib/docker/BUILD
work=$BASE/SqlInsights
git clone https://github.com/devizer/Universe.SqlInsights $work
cd $work
git reset --hard
git pull
export SYSTEM_ARTIFACTSDIRECTORY=$BASE/Artifacts.SqlInsights; mkdir -p "$SYSTEM_ARTIFACTSDIRECTORY"
export COMPRESSION_LEVEL=1
export SHORT_ARTIFACT_RIDS=
export SKIP_PUBLISH=True
export BUILD_REPOSITORY_URI=https://github.com/devizer/Universe.SqlInsights
export BUILD_REPOSITORY_LOCALPATH=$work
export W3API_NET=6.0 # what the heck
if [[ ! -d $BUILD_REPOSITORY_LOCALPATH/src/universe.sqlinsights.w3app/build ]]; then
   Say "BUILD w3app"
   pushd src/universe.sqlinsights.w3app
     try-and-retry yarn install
     yarn build
   popd
fi
cd scripts
pushd .. >/dev/null
Say "PWD = [$PWD]"
. scripts/Calc-Current-Version.sh 
set +eu
bash scripts/Build-Github-Release-Artifacts.sh
popd >/dev/null
