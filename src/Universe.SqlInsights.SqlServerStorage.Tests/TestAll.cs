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

        SqlServerSqlInsightsStorage CreateStorage(TestProvider provider)
        {
            var migrations = new SqlServerSqlInsightsMigrations(provider.Provider, provider.ConnectionString)
            {
                ThrowOnError = true
            };
            migrations.Migrate();
            return new SqlServerSqlInsightsStorage(provider.Provider, provider.ConnectionString);
        }

        [Test, TestCaseSource(typeof(TestProvider), nameof(TestProvider.GetList))]
        public async Task Test1_Seed(TestProvider provider)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(provider);
            var sessions = await storage.GetSessions();
            Console.WriteLine($"Sessions on-start: {sessions.Count()}");
            Seeder seeder = new Seeder(SqlClientFactory.Instance, storage.ConnectionString);
            await seeder.Seed();
            Console.WriteLine($"Sessions on-end: {(await storage.GetSessions()).Count()}");
        }

        [Test, TestCaseSource(typeof(TestProvider), nameof(TestProvider.GetList))]
        public async Task Test2_LoadAll(TestProvider provider)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(provider);
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

        [Test, TestCaseSource(typeof(TestProvider), nameof(TestProvider.GetList))]
        public async Task Test3_FinishAllSessions(TestProvider provider)
        {
            // Finish a single session
            SqlServerSqlInsightsStorage storage = CreateStorage(provider);
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

        [Test, TestCaseSource(typeof(TestProvider), nameof(TestProvider.GetList))]
        public async Task Test4_Rename_and_Delete_Session(TestProvider provider)
        {
            SqlServerSqlInsightsStorage storage = CreateStorage(provider);
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

    }
}