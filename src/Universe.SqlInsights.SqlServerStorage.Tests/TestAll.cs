using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;
using Universe.NUnitTests;
using Universe.SqlInsights.Shared;
using Universe.SqlServerJam;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class TestAll : NUnitTestsBase
    {

        private static bool NeedDropDatabaseOnDispose = true;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var raw = Environment.GetEnvironmentVariable("NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES");
            bool keep = "True".Equals(raw, StringComparison.OrdinalIgnoreCase);
            NeedDropDatabaseOnDispose = !keep;
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
        }

        SqlServerSqlInsightsStorage CreateStorage(DbProviderFactory provider, string connectionString, bool verboseLog)
        {
            SqlServerSqlInsightsStorage.ResetCacheForTests();
            
            if (TestContext.CurrentContext.Test.Arguments.FirstOrDefault() is TestCaseProvider testCase)
            {
                SqlServerSqlInsightsMigrations.DisableMemoryOptimizedTables = !testCase.NeedMot.GetValueOrDefault();
                /*
                Console.WriteLine($"{{{TestContext.CurrentContext.Test.Name}}}: DisableMemoryOptimizedTables {SqlServerSqlInsightsMigrations.DisableMemoryOptimizedTables}" +
                                  $", testCase.NeedMot={testCase.NeedMot}");
                Console.WriteLine("Test Case:" + Environment.NewLine + testCase);
            */
            }
            else
                throw new InvalidOperationException($"Test '{TestContext.CurrentContext.Test.Name}' should be parametrized by TestCaseSource attribute and TestCaseProvider argument");


            SqlServerDbExtensions.CreateDbIfNotExists(connectionString, TestEnv.OptionalDbDataDir, initialDataSize: 64);
            var migrations = new SqlServerSqlInsightsMigrations(provider, connectionString)
            {
                ThrowOnDbCreationError = true
            };
            migrations.Migrate();
            if (verboseLog)
                Console.WriteLine($"Migration successfully completed. Details:{Environment.NewLine}{migrations.Logs}");

            var dbName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
            if (NeedDropDatabaseOnDispose)
            OnDispose(
                $"Drop Database [{dbName}]",
                () => ResilientDbKiller.Kill(connectionString, throwOnError: false, retryCount: 3),
                TestDisposeOptions.Class
            );
            return new SqlServerSqlInsightsStorage(provider, connectionString);
        }


        SqlServerSqlInsightsStorage CreateStorage(TestCaseProvider testCase, bool verboseLog = false) => CreateStorage(testCase.Provider, testCase.ConnectionString, verboseLog);

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetTestCases))]
        public async Task Test0_Migrate(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase, true);
            // Next line breaks ram disk tests (2400M)
            // await storage.CreateSession("New Alive Session", null);
        }

        [Test, TestCaseSource(typeof(SeedTestCaseProvider), nameof(TestCaseProvider.GetTestCases))]
        public async Task Test1_Seed(SeedTestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase.Provider, testCase.ConnectionString, false);
            var sessions = await storage.GetSessions();
            Console.WriteLine($"Sessions on-start: {sessions.Count()}");
            Seeder seeder = new Seeder(testCase.Provider, storage.ConnectionString);
            await seeder.Seed(testCase.ThreadCount, testCase.LimitCount);
            Console.WriteLine($"Sessions on-end: {(await storage.GetSessions()).Count()}");
        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetTestCases))]
        public async Task Test1b_Any_Alive_Session_Exists(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            var idNewSession = await storage.CreateSession($"Test Session {Guid.NewGuid():N}", null);

            try
            {
                bool anyAliveSession = storage.AnyAliveSession();
                Assert.IsTrue(anyAliveSession);
            }
            finally
            {
                await storage.DeleteSession(idNewSession);
            }
        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetTestCases))]
        public async Task Test2_LoadAll(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            var sessions = await storage.GetSessions();
            int countCommands = 1;
            foreach (SqlInsightsSession session in sessions)
            {
                var ts1 = await storage.GetActionsSummaryTimestamp(session.IdSession);
                var summary = await storage.GetActionsSummary(session.IdSession);
                var keys = summary.Select(x => x.Key).ToArray();
                countCommands += 2;
                foreach (var key in keys)
                {
                    var timestamp = await storage.GetKeyPathTimestampOfDetails(session.IdSession, key);
                    var actionsByKey = await storage.GetActionsByKeyPath(session.IdSession, key);
                    // Dispose by foreach | ToList
                    foreach (var action in actionsByKey)
                    {
                    }
                    countCommands += 2;
                }
            }
            
            Console.WriteLine($"Commands: {countCommands}");
        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetTestCases))]
        [Ignore("Doesn't work. idSession == 0 check is on data access level")]
        public async Task Test3b_No_AliveSessions(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            await storage.FinishSession(0);
            bool anyAliveSession = storage.AnyAliveSession();
            Assert.IsFalse(anyAliveSession);
        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetTestCases))]
        public async Task Test3_FinishAllSessions(TestCaseProvider testCase)
        {
            // Finish a single session
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            var idNewSession = await storage.CreateSession($"Test Session {Guid.NewGuid():N}", null);
            var aliveSessions = (await storage.GetSessions()).Where(x => !x.IsFinished).ToList();
            
            if (aliveSessions.Count == 0)
                throw new InvalidOperationException("At least one alive session is required");

            foreach (var session in aliveSessions)
            {
                Console.WriteLine($"Ending session {session.IdSession}");
                await storage.FinishSession(session.IdSession);
                // break;
            }

            int aliveSessionsCount = storage.GetAliveSessions().Count();
            Assert.AreEqual(0, aliveSessionsCount, "Alive Session count should be 0");

            int aliveSessionsCount2 = storage.GetAliveSessions().Count(x => x != 0);
            Assert.AreEqual(0, aliveSessionsCount2, "Alive Session count (except default one) should be 0");

            await storage.DeleteSession(idNewSession);
        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetTestCases))]
        public async Task Test4_Rename_and_Delete_Session(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);

            var idSession = await storage.CreateSession("Session To Finish", null);
            await storage.FinishSession(idSession);

            SqlInsightsSession targetSession = (await storage.GetSessions())
                .Where(x => x.IsFinished && x.IdSession != 0)
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefault();
            
            if (targetSession == null)
                Assert.Fail("DeleteSession() test needs finished session");

            // Act "Rename"
            var expectedName = $"Tmp Session {Guid.NewGuid()}";
            await storage.RenameSession(targetSession.IdSession, expectedName);
            var actualName = (await storage.GetSessions())
                .Where(x => x.IdSession == targetSession.IdSession)
                .FirstOrDefault()?.Caption;

            // Assert "Rename"
            Assert.AreEqual(expectedName, actualName, "Renaming Session");
            
            // Act Delete
            await storage.DeleteSession(targetSession.IdSession);
            // Assert Delete
            var sessionsAfter = await storage.GetSessions();
            Assert.IsNull(sessionsAfter.FirstOrDefault(x => x.IdSession == targetSession.IdSession));

            await storage.DeleteSession(idSession);
        }
        
        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetTestCases))]
        public async Task Test5_Select_AppNames(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            Seeder seeder = new Seeder(testCase.Provider, storage.ConnectionString);
            await seeder.Seed(1, 2);

            // Assert
            var appNames = await storage.GetAppNames();
            Console.WriteLine(string.Join("; ", appNames.Select(x => $"{x.Id}='{x.Value}'")));
            bool contains = appNames.Any(x => x.Value == Seeder.TestAppName);
            Assert.True(contains, $"storage.GetAppNames() should return '{Seeder.TestAppName}'");
        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetTestCases))]
        public async Task Test6_Select_HostIds(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            Seeder seeder = new Seeder(testCase.Provider, storage.ConnectionString);
            await seeder.Seed(1, 1);

            // Assert
            var strings = await storage.GetHostIds();
            Console.WriteLine(string.Join("; ", strings.Select(x => $"{x.Id}='{x.Value}'")));
            bool contains = strings.Any(x => x.Value == Environment.MachineName);
            Assert.True(contains, $"storage.GetAppNames() should return '{Environment.MachineName}'");
        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetTestCases))]
        public async Task Test7_Test_Strings_Storage(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            using (IDbConnection con = testCase.Provider.CreateConnection())
            {
                con.ConnectionString = testCase.ConnectionString;
                StringsStorage ss = new StringsStorage(con, null);
                for (int len = StringsStorage.MaxStartLength - 5; len <= StringsStorage.MaxStartLength + 5; len++)
                {
                    var expected = GetRandomString(len);
                    IEnumerable<LongIdAndString> all = await ss.GetAllStringsByKind(StringKind.AppName);
                    Assert.IsFalse(all.Any(x => x.Value == expected), $"[Before] String is missing in the storage (len={len}) '{expected}'");
                    var idString = ss.AcquireString(StringKind.AppName, expected);
                    try
                    {
                        Assert.IsNotNull(idString);
                        IEnumerable<LongIdAndString> allAfter = await ss.GetAllStringsByKind(StringKind.AppName);
                        Assert.IsTrue(allAfter.Any(x => x.Value == expected), $"[After] String exists in the storage (len={len}) '{expected}'");
                    }
                    finally
                    {
                        await con.ExecuteAsync("Delete From SqlInsightsString Where IdString = @IdString", new { idString });
                    }
                }
            }
        }


        private static Random Rand = new Random(42);
        public static string GetRandomString(int len)
        {
            StringBuilder ret = new StringBuilder();
            for (int i = 0; i < len; i++)
                ret.Append((char)Rand.Next(32, 96 + 25));

            return ret.ToString();
        }
        
    }
    
}