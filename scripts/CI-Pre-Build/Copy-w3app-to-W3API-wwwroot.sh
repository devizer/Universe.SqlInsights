        cd src/universe.sqlinsights.w3app/build
        mkdir -p $SYSTEM_ARTIFACTSDIRECTORY/w3api/wwwroot
        cp -av * $SYSTEM_ARTIFACTSDIRECTORY/w3api/wwwroot/
        Say "CONTENT FOR [$SYSTEM_ARTIFACTSDIRECTORY/w3api]"
        ls -laR $SYSTEM_ARTIFACTSDIRECTORY/w3api
