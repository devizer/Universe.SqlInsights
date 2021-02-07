using System;
using System.Linq;
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
        public void Test1_Seed()
        {
            SqlServerSqlInsightsStorage storage = new SqlServerSqlInsightsStorage(ConnectionString);
            var sessions = storage.GetSessions();
            Console.WriteLine($"Sessions on-start: {sessions.Count()}");
            Seeder seeder = new Seeder(storage.ConnectionString);
            seeder.Seed();
            Console.WriteLine($"Sessions on-end: {storage.GetSessions().Count()}");
        }

        [Test]
        public void Test2_LoadAll()
        {
            SqlServerSqlInsightsStorage storage = new SqlServerSqlInsightsStorage(ConnectionString);
            var sessions = storage.GetSessions();
            int countCommands = 1;
            foreach (SqlInsightsSession session in sessions)
            {
                var ts1 = storage.GetActionsSummaryTimestamp(session.IdSession);
                var summary = storage.GetActionsSummary(session.IdSession);
                var keys = summary.Select(x => x.Key).ToArray();
                countCommands += 2;
                foreach (var key in keys)
                {
                    storage.GetKeyPathTimestampOfDetails(session.IdSession, key);
                    storage.GetActionsByKeyPath(session.IdSession, key);
                    countCommands += 2;
                }
            }
            
            Console.WriteLine($"Commands: {countCommands}");
            
        }

        [Test]
        public void Test3_FinishAllSessions()
        {
            // Finish a single session
            SqlServerSqlInsightsStorage storage = new SqlServerSqlInsightsStorage(ConnectionString);
            var aliveSessions = storage.GetSessions().Where(x => !x.IsFinished).ToList();
            
            if (aliveSessions.Count < 2)
                throw new InvalidOperationException("At least one alive session is required");

            foreach (var session in aliveSessions)
            {
                Console.WriteLine($"Ending session {session.IdSession}");
                storage.FinishSession(session.IdSession);
            }

            var aliveSessionsCount = storage.GetAliveSessions().Count();
            Assert.AreEqual(1, aliveSessionsCount, "Alive Session count should be 1");

            var aliveSessionsCount2 = storage.GetAliveSessions().Count(x => x != 0);
            Assert.AreEqual(0, aliveSessionsCount2, "Alive Session count (except default one) should be 0");

        }
    }
}