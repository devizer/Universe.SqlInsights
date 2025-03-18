using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Interfaces;

namespace Universe.NUnitPipeline.SqlServerDatabaseFactory
{
    public class DbTestPipeline
    {
        public static readonly string Title = "DB Test Pipeline";

        public static void OnStart(NUnitStage stage, ITest test)
        {
            if (stage.NUnitActionAppliedTo == NUnitActionAppliedTo.Test)
            {
                // var dbDefinishion = test.GetPropertyOrAdd<IDatabaseDefinition>("DatabaseDefinition", null);
                List<TestDbConnectionString> found = TestArgumentReflection.FindTestDbConnectionStrings(test.Arguments);
                PipelineLog.LogTrace($"[DbTestPipeline.OnStart] Test='{test.Name}' TestDbConnectionString Count: {found.Count}, PostponedCount: {found.Count(x => x.Postponed)}{Environment.NewLine}");
                TestDbConnectionString testDbConnectionString = found.FirstOrDefault();
                if (testDbConnectionString != null)
                {
                    // Move to DI
                    // SqlServerTestsConfiguration.Instance: <ISqlServerTestsConfiguration>
                    // GetNextTestDatabaseName: ITestDatabaseNameProvider

                    var testDatabaseNameProvider = NUnitPipelineConfiguration.GetService<ITestDatabaseNameProvider>();
                    var sqlServerTestsConfiguration = NUnitPipelineConfiguration.GetService<ISqlServerTestsConfiguration>();
                    SqlServerTestDbManager man = new SqlServerTestDbManager(sqlServerTestsConfiguration);


                    var testDbName = testDatabaseNameProvider.GetNextTestDatabaseName().GetSafeResult();

                    // Intro. Create New DB. Inject Connection String
                    PipelineLog.LogTrace($"[DbTestPipeline.OnStart] Test='{test.Name}' Creating Database '{testDbName}'");
                    // SeededEfDatabaseFactory is repsonsible for creating DB
                    // man.CreateEmptyDatabase(testDbName).SafeWait();
                    var connectionString = man.BuildConnectionString(testDbName, pooling: true);
                    // Finalizing Test DB (TestDbConnectionString)
                    testDbConnectionString.ConnectionString = connectionString;
                    PipelineLog.LogTrace($"[DbTestPipeline.OnStart] Test='{test.Name}' Database {testDbName}. Created. Connection String is '{connectionString}' Applying Migration and Seed '{testDbConnectionString.ManagedBy.Title}'");

                    SeededDatabaseFactory seedEfFactory = new SeededDatabaseFactory(sqlServerTestsConfiguration);
                    IDbConnectionString dbcs =
                        seedEfFactory.BuildDatabase(
                            testDbConnectionString.ManagedBy.CacheKey,
                            testDbName,
                            testDbConnectionString.ManagedBy.Title,
                            testDbConnectionString.ManagedBy.SavedDatabaseName,
                            testDbConnectionString.ManagedBy.MigrateAndSeed
                        ).GetSafeResult();


                    var whenDeleteDb = TestDisposeOptions.AsyncGlobal;

                    if (!NeedKeepTempTestDatabases())
                        TestCleaner.OnDispose($"Drop DB '{testDbName}'", () => man.DropDatabase(testDbName).SafeWait(), whenDeleteDb);

                    return;

                    // This work without backup cache
                    PipelineLog.LogTrace($"[DbTestPipeline.OnStart] Test='{test.Name}' Migrating and seeding database {testDbName}");
                    testDbConnectionString.ManagedBy.MigrateAndSeed(testDbConnectionString);
                    PipelineLog.LogTrace($"[DbTestPipeline.OnStart] Test='{test.Name}' Completed database {testDbName}");
                    TestCleaner.OnDispose($"Drop DB '{testDbName}' for Test {test.Name}", () => man.DropDatabase(testDbName).SafeWait(), TestDisposeOptions.Global);

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

        protected internal static bool NeedKeepTempTestDatabases()
        {
            var raw = Environment.GetEnvironmentVariable("NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES");
            return
                !string.IsNullOrEmpty(raw)
                && (raw.Equals("1", StringComparison.OrdinalIgnoreCase)
                    || raw.Equals("True", StringComparison.OrdinalIgnoreCase)
                    || raw.Equals("On", StringComparison.OrdinalIgnoreCase)
                );
        }

    }
}
