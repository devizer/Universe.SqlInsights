using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class Tests
    {

        [OneTimeSetUp]
        public void Setup()
        {
        }

        SqlServerSqlInsightsStorage CreateStorage(TestCaseProvider testCase)
        {
            var migrations = new SqlServerSqlInsightsMigrations(testCase.Provider, testCase.ConnectionString)
            {
                ThrowOnDbCreationError = true
            };
            migrations.Migrate();
            return new SqlServerSqlInsightsStorage(testCase.Provider, testCase.ConnectionString);
        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetList))]
        public async Task Test1_Seed(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            var sessions = await storage.GetSessions();
            Console.WriteLine($"Sessions on-start: {sessions.Count()}");
            Seeder seeder = new Seeder(SqlClientFactory.Instance, storage.ConnectionString);
            await seeder.Seed();
            Console.WriteLine($"Sessions on-end: {(await storage.GetSessions()).Count()}");
        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetList))]
        public void Test1b_Any_Alive_Session_Exists(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            bool anyAliveSession = storage.AnyAliveSession();
            Assert.IsTrue(anyAliveSession);
        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetList))]
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
                    await storage.GetKeyPathTimestampOfDetails(session.IdSession, key);
                    await storage.GetActionsByKeyPath(session.IdSession, key);
                    countCommands += 2;
                }
            }
            
            Console.WriteLine($"Commands: {countCommands}");
        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetList))]
        [Ignore("Doesn't work. idSession == 0 check is on data access level")]
        public async Task Test3b_No_AliveSessions(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            await storage.FinishSession(0);
            bool anyAliveSession = storage.AnyAliveSession();
            Assert.IsFalse(anyAliveSession);
        }
        

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetList))]
        public async Task Test3_FinishAllSessions(TestCaseProvider testCase)
        {
            // Finish a single session
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            var id = await storage.CreateSession("Test Session", null);
            var aliveSessions = (await storage.GetSessions()).Where(x => !x.IsFinished).ToList();
            
            if (aliveSessions.Count < 2)
                throw new InvalidOperationException("At least one alive session is required");

            foreach (var session in aliveSessions)
            {
                Console.WriteLine($"Ending session {session.IdSession}");
                await storage.FinishSession(session.IdSession);
                // break;
            }

            int aliveSessionsCount = storage.GetAliveSessions().Count();
            Assert.AreEqual(1, aliveSessionsCount, "Alive Session count should be 1");

            int aliveSessionsCount2 = storage.GetAliveSessions().Count(x => x != 0);
            Assert.AreEqual(0, aliveSessionsCount2, "Alive Session count (except default one) should be 0");

        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetList))]
        public async Task Test4_Rename_and_Delete_Session(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            SqlInsightsSession targetSession = (await storage.GetSessions())
                .Where(x => x.IsFinished && x.IdSession != 0)
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefault();
            
            if (targetSession == null)
                Assert.Fail("DeleteSession() test needs finished session");

            var expectedName = $"Tmp Sess {Guid.NewGuid()}";
            await storage.RenameSession(targetSession.IdSession, expectedName);
            var actualName = (await storage.GetSessions())
                .Where(x => x.IdSession == targetSession.IdSession)
                .FirstOrDefault()?.Caption;

            Assert.AreEqual(expectedName, actualName, "Renaming Session");
            
            await storage.DeleteSession(targetSession.IdSession);
            var sessionsAfter = await storage.GetSessions();
            Assert.IsNull(sessionsAfter.FirstOrDefault(x => x.IdSession == targetSession.IdSession));

        }
        
        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetList))]
        public async Task Test5_Select_AppNames(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            var appNames = await storage.GetAppNames();
            Console.WriteLine(string.Join("; ", appNames.Select(x => $"{x.Id}='{x.Value}'")));
            bool contains = appNames.Any(x => x.Value == Seeder.TestAppName);
            Assert.True(contains, $"storage.GetAppNames() should return '{Seeder.TestAppName}'");
        }

        [Test, TestCaseSource(typeof(TestCaseProvider), nameof(TestCaseProvider.GetList))]
        public async Task Test6_Select_HostIds(TestCaseProvider testCase)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(testCase);
            var strings = await storage.GetHostIds();
            Console.WriteLine(string.Join("; ", strings.Select(x => $"{x.Id}='{x.Value}'")));
            bool contains = strings.Any(x => x.Value == Environment.MachineName);
            Assert.True(contains, $"storage.GetAppNames() should return '{Environment.MachineName}'");
        }
    }
}