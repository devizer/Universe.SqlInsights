. "$PSScriptRoot\Functions.ps1"

      cd Goods\AdministrativeViews.tests
      # Get-ChildItem | Format-Table -AutoSize
      dotnet test Universe.SqlServer.AdministrativeViews.Tests.dll

