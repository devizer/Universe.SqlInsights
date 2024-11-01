using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using Universe.NUnitPipeline;

namespace Library.Implementation
{
    internal class DbTestPipeline
    {
        public static readonly string Title = "DB Test Pipeline";

        public static void OnStart(NUnitStage stage, ITest test)
        {
            if (stage.NUnitActionAppliedTo == NUnitActionAppliedTo.Test)
            {
                // var dbDefinishion = test.GetPropertyOrAdd<IDatabaseDefinition>("DatabaseDefinition", null);
                List<TestDbConnectionString> found = TestArgumentReflection.FindTestDbConnectionStrings(test.Arguments);
                TempDebug.WriteLine($"[DbTestPipeline.OnStart] Test='{test.Name}' TestDbConnectionString Count: {found.Count}, PostponedCount: {found.Count(x => x.Postponed)}{Environment.NewLine}" );
                TestDbConnectionString? testDbConnectionString = found.FirstOrDefault();
                if (testDbConnectionString != null)
                {
                    SqlServerTestDbManager man = new SqlServerTestDbManager(SqlServerTestsConfiguration.Instance);
                    var testDbName = man.GetNextTestDatabaseName().Result;

                    TempDebug.WriteLine($"[DbTestPipeline.OnStart] Test='{test.Name}' Creating Database {testDbName}");
                    man.CreateEmptyDatabase(testDbName).Wait();
                    var connectionString = man.BuildConnectionString(testDbName, pooling: true);
                    // Finalizing Test DB (TestDbConnectionString)
                    testDbConnectionString.ConnectionString = connectionString;

                    TempDebug.WriteLine($"[DbTestPipeline.OnStart] Test='{test.Name}' Migrating and seeding database {testDbName}");
                    testDbConnectionString.ManagedBy.MigrateAndSeed(testDbConnectionString);
                    TempDebug.WriteLine($"[DbTestPipeline.OnStart] Test='{test.Name}' Completed database {testDbName}");
                    TestCleaner.OnDispose($"Drop DB '{testDbName}' for Test {test.Name}", () => man.DropDatabase(testDbName).Wait(), TestDisposeOptions.Global);

                    TempDebug.WriteLine($"[DbTestPipeline.OnStart] Test='{test.Name}' Connection String is \"{connectionString}\"");


                }

            }
        }

        public static void OnFinish(NUnitStage stage, ITest test)
        {
            if (stage.NUnitActionAppliedTo == NUnitActionAppliedTo.Test)
            {
            }
        }
    }
}
