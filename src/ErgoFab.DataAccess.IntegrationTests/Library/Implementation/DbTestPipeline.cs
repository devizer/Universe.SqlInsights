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
                PipelineLog.LogTrace($"[DbTestPipeline.OnStart] Test='{test.Name}' TestDbConnectionString Count: {found.Count}, PostponedCount: {found.Count(x => x.Postponed)}{Environment.NewLine}" );
                TestDbConnectionString? testDbConnectionString = found.FirstOrDefault();
                if (testDbConnectionString != null)
                {
                    // Move to DI
                    // SqlServerTestsConfiguration.Instance: <ISqlServerTestsConfiguration>
                    // GetNextTestDatabaseName: ITestDatabaseNameProvider
                    SqlServerTestDbManager man = new SqlServerTestDbManager(SqlServerTestsConfiguration.Instance);
                    var testDbName = man.GetNextTestDatabaseName().GetSafeResult();

                    // Intro. Create New DB. Inject Connection String
                    PipelineLog.LogTrace($"[DbTestPipeline.OnStart] Test='{test.Name}' Creating Database '{testDbName}'");
                    // SeededEfDatabaseFactory is repsonsible for creating DB
                    // man.CreateEmptyDatabase(testDbName).SafeWait();
                    var connectionString = man.BuildConnectionString(testDbName, pooling: true);
                    // Finalizing Test DB (TestDbConnectionString)
                    testDbConnectionString.ConnectionString = connectionString;
                    PipelineLog.LogTrace($"[DbTestPipeline.OnStart] Test='{test.Name}' Database {testDbName}. Created. Connection String is '{connectionString}' Applying Migration and Seed '{testDbConnectionString.ManagedBy.Title}'");

                    SeededEfDatabaseFactory seedEfFactory = new SeededEfDatabaseFactory(SqlServerTestsConfiguration.Instance);
                    IDbConnectionString dbcs =
                        seedEfFactory.BuildEfDatabase(
                            testDbConnectionString.ManagedBy.CacheKey,
                            testDbName,
                            testDbConnectionString.ManagedBy.Title,
                            cs => cs.CreateErgoFabDbContext(),
                            testDbConnectionString.ManagedBy.MigrateAndSeed
                        ).GetSafeResult();


                    var whenDeleteDb = TestDisposeOptions.AsyncGlobal;
                    TestCleaner.OnDispose($"Drop DB '{testDbName}'", man.DropDatabase(testDbName).SafeWait, whenDeleteDb);

                    return;

                    // This work without backup cache
                    PipelineLog.LogTrace($"[DbTestPipeline.OnStart] Test='{test.Name}' Migrating and seeding database {testDbName}");
                    testDbConnectionString.ManagedBy.MigrateAndSeed(testDbConnectionString);
                    PipelineLog.LogTrace($"[DbTestPipeline.OnStart] Test='{test.Name}' Completed database {testDbName}");
                    TestCleaner.OnDispose($"Drop DB '{testDbName}' for Test {test.Name}", () => man.DropDatabase(testDbName).Wait(), TestDisposeOptions.Global);

                    PipelineLog.LogTrace($"[DbTestPipeline.OnStart] Test='{test.Name}' Connection String is \"{connectionString}\"");


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
