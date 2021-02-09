using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Universe.SqlInsights.Shared;
using Universe.SqlTrace;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    class Seeder
    {
        public readonly string ConnectionString;

        public Seeder(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public async Task Seed()
        {
            
            SqlServerSqlInsightsStorage storage = new SqlServerSqlInsightsStorage(ConnectionString);
            var sessions = await storage.GetSessions();
            if (sessions.Count(x => !x.IsFinished) < 2)
            {
                Console.WriteLine("Create 2 Sessions: limited and unlimited");
                var session1 = await storage.CreateSession("Snapshot Un-Limited" + DateTime.UtcNow, null);
                var session2 = await storage.CreateSession("Snapshot Limited" + DateTime.UtcNow, 60*24*7);
                Assert.AreNotEqual(session1, 0, "Session1 is not zero");
                Assert.AreNotEqual(session2, 0, "Session2 is not zero");
            }
            
            // Seed actions
            for (int i = 65; i < 90; i++)
            {
                SqlInsightsActionKeyPath key = new SqlInsightsActionKeyPath("TESTS", "Act " + ((char)i));
                ActionDetailsWithCounters.SqlStatement stm = new ActionDetailsWithCounters.SqlStatement()
                {
                    Counters = new SqlCounters() {Duration = 42}
                };

                for (int j = 0; j < 10 * (i - 65); j++)
                {
                    storage.AddAction(new ActionDetailsWithCounters()
                    {
                        Key = key,
                        At = DateTime.UtcNow,
                        AppDuration = 2,
                        AppName = "Tests",
                        HostId = Environment.MachineName,
                        IsOK = true,
                        SqlStatements = {stm}
                    });
                }
            }
            
        }
    }
}