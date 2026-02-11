
      & dotnet tool install --global SqlServer.AdministrativeViews
      $output_folder = "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Administrative Views"
      $output_file = "$output_folder\{InstanceName} {Version} on {Platform}"
      
      & SqlServer.AdministrativeViews -all -o "$output_file"
      Say "Report Files at [$output_folder]"
      Get-ChildItem "$output_folder" -EA SilentlyContinue | Format-Table -AutoSize | Out-Host
