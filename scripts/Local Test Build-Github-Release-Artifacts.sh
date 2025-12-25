work=$HOME/SqlInsights
git clone https://github.com/devizer/Universe.SqlInsights $work
cd $work
git reset --hard
git pull
cd scripts
export SYSTEM_ARTIFACTSDIRECTORY=$HOME/Artifacts.SqlInsights
export COMPRESSION_LEVEL=1
export SHORT_ARTIFACT_RIDS=
export SKIP_PUBLISH=True
bash Build-Github-Release-Artifacts.sh
