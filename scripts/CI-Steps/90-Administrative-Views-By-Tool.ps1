. "$PSScriptRoot\Functions.ps1"

      & dotnet tool install --global SqlServer.AdministrativeViews
      $output_folder = "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Administrative Views"
      $output_file = "$output_folder\{InstanceName} {Version} on {Platform}"
      
      if ((Get-Os-Platform) -eq "Windows") {
         & SqlServer.AdministrativeViews -all -o "$output_file"
      } Else {
        & SqlServer.AdministrativeViews -cs "$ENV:SQLINSIGHTS_CONNECTION_STRING" -o "$output_file"
      }
      Show-Last-Exit-Code "TOOL SqlServer.AdministrativeViews" -Throw

      Say "Report Files at [$output_folder]"
      Get-ChildItem "$output_folder" -EA SilentlyContinue | Format-Table -AutoSize | Out-Host
