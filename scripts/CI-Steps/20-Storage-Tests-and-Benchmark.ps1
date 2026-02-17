. "$PSScriptRoot\Functions.ps1"

      cd Goods\w3api.tests

      Say "Full Storage Test With Coverage"
      if (-not "$ENV:RAM_DISK") { $ENV:NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES = "True" } # for query cache
      $data_dir="C:\SQL-Data"
      if (Test-Path "D:\") { $data_dir="D:\SQL-Data" }
      if ((Get-Os-Platform) -ne "Windows") { $data_dir = $null }
      $ENV:SQLINSIGHTS_DATA_DIR = "$data_dir"
      $ENV:TEST_CPU_NAME = "$(Get-Cpu-Name)"
      $ENV:TEST_CONFIGURATION = "DISK";
      if ("$ENV:DB_DATA_DIR") {
        $ENV:SQLINSIGHTS_DATA_DIR = "$ENV:DB_DATA_DIR"
        $ENV:TEST_CONFIGURATION = "RAM-Disk"
        Say "SQLINSIGHTS_DATA_DIR is $ENV:SQLINSIGHTS_DATA_DIR"
      }
      $ENV:TEST_UNDER_COVERAGE = "True"
      & dotnet test --collect:"XPlat Code Coverage" --logger trx Universe.SqlInsights.SqlServerStorage.Tests.dll

      Say "Benchmark Seed"
      Remove-Item -Path "$Env:SYSTEM_ARTIFACTSDIRECTORY/AddAction.log" -ErrorAction SilentlyContinue -Force
      $ENV:TEST_UNDER_COVERAGE = "False"
      & dotnet test --filter "Name~Test1_Seed" Universe.SqlInsights.SqlServerStorage.Tests.dll
