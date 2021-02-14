using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class Tests
    {
        // private string ConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Database=SqlServerSqlInsightsStorage_Tests; Integrated Security=SSPI";
        private string ConnectionString = "Data Source=(local);Database=SqlServerSqlInsightsStorage_Tests; Integrated Security=SSPI";

        [OneTimeSetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Test1_Seed()
        {
            SqlServerSqlInsightsStorage storage = new SqlServerSqlInsightsStorage(ConnectionString);
            var sessions = await storage.GetSessions();
            Console.WriteLine($"Sessions on-start: {sessions.Count()}");
            Seeder seeder = new Seeder(storage.ConnectionString);
            await seeder.Seed();
            Console.WriteLine($"Sessions on-end: {(await storage.GetSessions()).Count()}");
        }

        [Test]
        public async Task Test2_LoadAll()
        {
            SqlServerSqlInsightsStorage storage = new SqlServerSqlInsightsStorage(ConnectionString);
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

        [Test]
        public async Task Test3_FinishAllSessions()
        {
            // Finish a single session
            SqlServerSqlInsightsStorage storage = new SqlServerSqlInsightsStorage(ConnectionString);
            var aliveSessions = (await storage.GetSessions()).Where(x => !x.IsFinished).ToList();
            
            if (aliveSessions.Count < 2)
                throw new InvalidOperationException("At least one alive session is required");

            foreach (var session in aliveSessions)
            {
                Console.WriteLine($"Ending session {session.IdSession}");
                await storage.FinishSession(session.IdSession);
            }

            int aliveSessionsCount = storage.GetAliveSessions().Count();
            Assert.AreEqual(1, aliveSessionsCount, "Alive Session count should be 1");

            int aliveSessionsCount2 = storage.GetAliveSessions().Count(x => x != 0);
            Assert.AreEqual(0, aliveSessionsCount2, "Alive Session count (except default one) should be 0");

        }

        [Test]
        public async Task Test4_DeleteSession()
        {
            SqlServerSqlInsightsStorage storage = new SqlServerSqlInsightsStorage(ConnectionString);
            SqlInsightsSession targetSession = (await storage.GetSessions())
                .Where(x => x.IsFinished && x.IdSession != 0)
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefault();
            
            if (targetSession == null)
                Assert.Fail("DeleteSession() test needs finished session");

            await storage.DeleteSession(targetSession.IdSession);
            var sessionsAfter = await storage.GetSessions();
            Assert.IsNull(sessionsAfter.FirstOrDefault(x => x.IdSession == targetSession.IdSession));



        }

    }
}