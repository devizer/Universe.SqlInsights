        Import-DevOps
        $ErrorActionPreference="Continue"
        echo "SQL: '$($ENV:sql)'"

        Say "Restarting Host Network Service"
        try { Restart-Service hns } catch {}
        Say "docker network prune"
        & docker network prune
        Say "docker system prune -f"
        & docker system prune -f

        $image="devizervlad/iis-net4x-net35:$($ENV:SQL_IMAGE_TAG)"
        # Say "Pull $image ..."
        # Try-And-Retry "PULL $image" "& docker pull $image | out-host"
        # & docker.exe pull "$image"; & docker.exe pull "$image"; & docker.exe pull "$image"; 


        Say "Starting container sql-server, SQL_IMAGE_ISOLATION = [$($ENV:SQL_IMAGE_ISOLATION)]"
        & docker rm -f sql-server 2>$null
        New-item C:\SQL -ItemType Directory -Force -EA SilentlyContinue | Out-Null
        New-item C:\Temp -ItemType Directory -Force -EA SilentlyContinue | Out-Null
        $mnt="type=bind,source=$(Get-Location),target=C:\App"
        echo "--mount parameter is: [$mnt]"
        echo "SYSTEM_ARTIFACTSDIRECTORY = [$($ENV:SYSTEM_ARTIFACTSDIRECTORY)]"
        $cpuCount="$([Environment]::ProcessorCount)"
        # disk D in missing in container
        # --mount "type=bind,source=$($ENV:SYSTEM_ARTIFACTSDIRECTORY),target=$($ENV:SYSTEM_ARTIFACTSDIRECTORY)" `
        # bad: --mount "type=bind,source=C:\SQL,target=C:\SQL" `
        $w3api_port=50420
        & docker run -d --name sql-server --memory 3700M --cpus "$cpuCount" "--isolation=$($ENV:SQL_IMAGE_ISOLATION)" `
          --hostname MSSQL `
          --storage-opt "size=50GB" `
          -e SQL="$($ENV:sql)" `
          -e TF_BUILD=True `
          -e SQL_IMAGE_TAG="$($ENV:SQL_IMAGE_TAG)" `
          -e SYSTEM_ARTIFACTSDIRECTORY="$($ENV:SYSTEM_ARTIFACTSDIRECTORY)" `
          -e SYSTEM_ARTIFACTSDIRECTORY="C:\ARTIFACTS" `
          --mount "type=bind,source=$($ENV:SYSTEM_ARTIFACTSDIRECTORY),target=C:\ARTIFACTS" `
          --mount "$mnt" `
          --mount "type=bind,source=C:\Temp,target=C:\Temp" `
          -e PS1_TROUBLE_SHOOT="On" -e SQLSERVERS_SETUP_FOLDER="C:\SQL-Setup" `
          -p "${w3api_port}:${w3api_port}" --workdir=C:\App --entrypoint powershell $image -Command "Write-Host WAITING; Sleep 214748; Wait-Event;"
        Sleep 3
        Say "CONTAINER STARTUP LOGS"
        docker logs --since 0 sql-server
        Say "Starting Setup in Container"
        & docker exec sql-server powershell -Command "cd C:\App; Write-Line -TextGreen 'Success: Container is Running and Has C:\App';" |
          tee-object "$ENV:SYSTEM_ARTIFACTSDIRECTORY/OUTPUT from CONTAINER.txt"
