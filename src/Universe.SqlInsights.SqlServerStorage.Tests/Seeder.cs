﻿using System;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Universe.SqlInsights.Shared;
using Universe.SqlTrace;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    class Seeder
    {
        public const string TestAppName = "Tests";
        public readonly DbProviderFactory ProviderFactory;
        public readonly string ConnectionString;

        public Seeder(DbProviderFactory providerFactory, string connectionString)
        {
            ProviderFactory = providerFactory;
            ConnectionString = connectionString;
        }

        public async Task Seed()
        {
            
            SqlServerSqlInsightsStorage storage = new SqlServerSqlInsightsStorage(ProviderFactory, ConnectionString);
            var sessions = await storage.GetSessions();
            if (sessions.Count(x => !x.IsFinished) < 2)
            {
                Console.WriteLine("Create 2 Sessions: limited and unlimited");
                var session1 = await storage.CreateSession("Snapshot Un-Limited " + DateTime.UtcNow, null);
                var session2 = await storage.CreateSession("Snapshot Limited " + DateTime.UtcNow, 60*24*7);
                Assert.AreNotEqual(session1, 0, "Session1 is not zero");
                Assert.AreNotEqual(session2, 0, "Session2 is not zero");
            }

            int aliveSessionsCount = storage.GetAliveSessions().Count();
            Stopwatch sw = Stopwatch.StartNew();
            int total = 0;
            
            // Seed actions
            for (int i = 65; i < 90; i++)
            {
                SqlInsightsActionKeyPath key = new SqlInsightsActionKeyPath(TestAppName, "Act " + ((char)i));
                ActionDetailsWithCounters.SqlStatement stm = new ActionDetailsWithCounters.SqlStatement()
                {
                    Counters = new SqlCounters() {Duration = 42}
                };

                for (int j = 0; j < 10 * (i - 65); j++)
                {
                    total += aliveSessionsCount;
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

            var ops = (double) total / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"OPS = {ops:n6}");

        }
    }
}