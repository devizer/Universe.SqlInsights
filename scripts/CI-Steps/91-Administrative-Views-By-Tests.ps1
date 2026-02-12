. "$PSScriptRoot\Functions.ps1"

      cd C:\App\Goods\AdministrativeViews.tests
      # Get-ChildItem | Format-Table -AutoSize
      dotnet test Universe.SqlServer.AdministrativeViews.Tests.dll

