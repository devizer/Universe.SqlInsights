      set -eu; set -o pipefail;
      Say "CPU: $(Get-CpuName)"

  Say "Install .net dependencies"
  Run-Remote-Script https://raw.githubusercontent.com/devizer/glist/master/install-dotnet-dependencies.sh
  sudo apt-get update -qq; sudo apt-get install libkrb5-3 zlib1g libunwind8 libuuid1 -y

  Say "Kerberos (libgss,libkrb) binaries: "
  ldconfig -p | { grep "libgss\|libkrb" || true; }

  Say "Kerberos (libgss,libkrb) packages: "
  list-packages | { grep "libkrb\|gss" || true; }

  Say "Existing Lib SSL binaries"
  ldconfig -p | { grep "libssl\|libcrypto" || true; }
  Say "Install Lib SSL 1.1 if required"
  export INSTALL_DIR=/usr/local/libssl-1.1
  sudo mkdir -p $INSTALL_DIR
  printf "\n$INSTALL_DIR\n" | sudo tee -a /etc/ld.so.conf >/dev/null || true
  Run-Remote-Script https://raw.githubusercontent.com/devizer/glist/master/install-libssl-1.1.sh
  sudo ldconfig || true
  ldconfig || true
  ldconfig -p | { grep "libssl\|libcrypto" || true; }
  export INSTALL_DIR=

      Say "Starting LINUX Container"
      v="";
      test -n "${RAM_DISK:-}" && v="-v ${RAM_DISK}:${RAM_DISK} -e MSSQL_DATA_DIR=${RAM_DISK}"
      echo "SQL Server Container Volume Options: [$v]"
      img="$DOCKER_IMAGE_FULL"
      Say "Pull image [$img] and run container"
      try-and-retry docker pull -q "$img"
      sudo mkdir -p /mnt/ergo-fab-tests
      sudo chmod 777 -R /mnt/ergo-fab-tests

      # --privileged --privileged --privileged --privileged --privileged --privileged --privileged --privileged
      v="$v -v /tmp/SqlInsights-Traces:/tmp/SqlInsights-Traces"
      mkdir -p /tmp/SqlInsights-Traces
      sudo chmod 777 -R /tmp/SqlInsights-Traces
      docker rm -f sqlserver 2>/dev/null
      password='p@assw0rd!'
      edition="${LINUX_MSSQL_PID:-Express}"
      docker run --privileged --pull never --restart on-failure:666 --name sqlserver \
         $v \
         -v /mnt/ergo-fab-tests:/mnt/ergo-fab-tests \
         -e "ACCEPT_EULA=Y" \
         -e "MSSQL_SA_PASSWORD=$password" \
         -e "MSSQL_PID=$edition" \
         -p 1433:1433 -d "$img"
      docker exec -t sqlserver ls -la /tmp


      export SQL_SERVER_CONTAINER_NAME=sqlserver # Add SqlCmd to Path for Linux Container and Wait for
      export SQL_PING_TIMEOUT=30 SQL_PING_PARAMETERS="-C -S localhost -U sa -P \""$password"\""
      Run-Remote-Script https://raw.githubusercontent.com/devizer/Universe.SqlServerJam/master/Add-SqlCmd-to-Path-for-Linux-Container.sh

      set +e
      Say "SQLCMD version:"
      docker exec -t sqlserver sqlcmd -? | head -3
      Say "Show data folder"
      docker exec -t sqlserver sqlcmd -C -S localhost -U sa -P "$password" -Q "Create Database My_DB_1; Exec(N'Use My_DB_1; Select size, physical_name From sys.database_files;'); exec sp_databases; Drop Database My_DB_1; exec sp_databases;"
      Say "Show fn_helpcollations"
      docker exec -t sqlserver sqlcmd -C -W -w 10000 -S localhost -U sa -P "$password" -Q "SELECT Name, Description FROM fn_helpcollations() order by 1" |& tee $SYSTEM_ARTIFACTSDIRECTORY/fn_helpcollations.txt
      Say "DOCKER ALIVE CONTAINERS"
      docker ps
      Say "DOCKER IMAGES"
      docker image ls

      # 2022: /etc/ld.so.conf.d/mssql.conf
      # 2019: /etc/ld.so.conf.d/mssql.conf
      for file in mssql.conf sqlservr; do
        Say "FIND $file"
        docker exec -t sqlserver bash -c "find / -name $file 2>/dev/null"
      done

      Say "/var/opt/mssql/mssql.conf"
      # /usr/src/app/do-my-sql-commands.sh & /opt/mssql/bin/sqlservr
      docker exec -t sqlserver cat /var/opt/mssql/mssql.conf
      Say "/opt/mssql/bin/sqlservr"
      docker exec -t sqlserver file /opt/mssql/bin/sqlservr || true
