      $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Title="CPU"
      $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Kind="String"
      $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Value="$(Get-Cpu-Name)"
      $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Position="Header"
      if ("$($ENV:DB_DATA_DIR)") {
        $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Title="Data RAM Disk (MB)"
        $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Kind="Natural"
        $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Value="$($ENV:RAM_DISK_SIZE)"
        $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Position="9999"
      }

      & dotnet tool install --global SqlServer.AdministrativeViews
      $output_folder = "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Administrative Views"
      $output_file = "$output_folder\{InstanceName} {Version} on {Platform}"
      
      & SqlServer.AdministrativeViews -all -o "$output_file"
      Say "Report Files at [$output_folder]"
      Get-ChildItem "$output_folder" -EA SilentlyContinue | Format-Table -AutoSize | Out-Host
