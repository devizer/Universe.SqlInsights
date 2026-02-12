        cd src/universe.sqlinsights.w3app/build
        Say "Copying from $(pwd -P)"
        mkdir -p $SYSTEM_ARTIFACTSDIRECTORY/w3api/wwwroot
        cp -av * $SYSTEM_ARTIFACTSDIRECTORY/w3api/wwwroot/
