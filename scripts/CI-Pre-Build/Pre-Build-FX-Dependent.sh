          configuration=Release

          Say "Build fx-dependent [ErgoFab.DataAccess.IntegrationTests]"
          pushd src/ErgoFab.DataAccess.IntegrationTests
          time try-and-retry dotnet publish -c $configuration -o $SYSTEM_ARTIFACTSDIRECTORY/ergofab.tests -f net8.0
          popd

          Say "Build fx-dependent W3API"
          pushd src/Universe.SqlInsights.W3Api
          time try-and-retry dotnet publish -c $configuration -o $SYSTEM_ARTIFACTSDIRECTORY/w3api -f net6.0
          popd

          Say "Build fx-dependent [SqlServerStorage].Tests"
          pushd src/Universe.SqlInsights.SqlServerStorage.Tests
          time try-and-retry dotnet publish -c $configuration -o $SYSTEM_ARTIFACTSDIRECTORY/w3api.tests -f net6.0
          popd

          Say "Build fx-dependent [https://github.com/devizer/Universe.SqlServerJam].Tests"
          git clone https://github.com/devizer/Universe.SqlServerJam ~/jam
          pushd ~/jam/src/Universe.SqlServerJam.Tests
          time try-and-retry dotnet publish -c $configuration -o $SYSTEM_ARTIFACTSDIRECTORY/jam.tests -f net6.0
          popd

          Say "Build fx-dependent [https://github.com/devizer/Universe.SqlServer.AdministrativeViews].Tests"
          git clone https://github.com/devizer/Universe.SqlServer.AdministrativeViews ~/AdministrativeViews
          pushd ~/AdministrativeViews/Universe.SqlServer.AdministrativeViews.Tests
          time try-and-retry dotnet publish -c $configuration -o $SYSTEM_ARTIFACTSDIRECTORY/AdministrativeViews.tests -f net8.0
          popd
