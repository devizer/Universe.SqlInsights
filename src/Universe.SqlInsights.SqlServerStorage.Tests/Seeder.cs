using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Universe.SqlInsights.Shared;
using Universe.SqlServerJam;
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

        public async Task Seed(int numThreads = 8, int limitCount = 99999)
        {
            Console.WriteLine($"TestCaseProvider.DbDataDir: [{TestEnv.OptionalDbDataDir}]");
            StringsStorage.ResetCacheForTests();
            MetadataCache.ResetCacheForTests();
            SqlServerSqlInsightsStorage.DebugAddAction = false;
            SqlServerSqlInsightsStorage storage = new SqlServerSqlInsightsStorage(ProviderFactory, ConnectionString);
            var sessions = await storage.GetSessions();
            foreach (var session in sessions.Where(x => x.IdSession != 0))
                await storage.DeleteSession(session.IdSession);
            
            foreach (var session in (await storage.GetSessions()).Where(x => x.IdSession != 0))
                await storage.DeleteSession(session.IdSession);

            int aliveSessionsCount = storage.GetAliveSessions().Count();
            Console.WriteLine($"Alive Sessions Count: {aliveSessionsCount}");
            
            SqlInsightsActionKeyPath key = new SqlInsightsActionKeyPath(TestAppName, $"Warm up");
            storage.AddAction(CreateActionDetailsWithCounters(key));
            // Console.WriteLine($"Warm Up completed (AddAction)");
            
            Stopwatch sw = Stopwatch.StartNew();
            int total = 0, fail = 0;
            
            // Seed actions
            
            ConcurrentDictionary<string,CounterHolder> errors = new ConcurrentDictionary<string, CounterHolder>();
            ParallelOptions po = new ParallelOptions() { MaxDegreeOfParallelism = numThreads };
            Parallel.ForEach(GetSeedingBatch().Take(limitCount), po, action =>
            {
                total++;
                try
                {
                    storage.AddAction(action);
                }
                catch (Exception ex)
                {
                    fail++;
                    var sqlError = ex.FindSqlError();
                    string err = (sqlError == null ? "" : $"#{sqlError.Number} ") + ex.Message;
                    var counter = errors.GetOrAdd(err, key => new CounterHolder());
                    counter.Total += 1;
                }
            });

            var ops = (double) total / sw.Elapsed.TotalSeconds;
            var providerName = ProviderFactory.GetShortProviderName();
            var errorsInfo = errors.Count == 0 ? "" : $"{Environment.NewLine}{string.Join(",", errors.OrderByDescending(x => x.Value.Total).Select(x => $"N={x.Value.Total}: {x.Key}"))}";
            Console.WriteLine($"[{providerName}] OPS = {ops:n1} actions per second. Adding Count: {total}. Fail count: {fail} (Cores: {Environment.ProcessorCount}, Threads: {numThreads}){errorsInfo}");
            if (total > 129)
            {
                var cnn = this.ProviderFactory.CreateConnection();
                cnn.ConnectionString = this.ConnectionString;
                var sqlServerManagement = cnn.Manage();
                var sqlVersion = sqlServerManagement.ShortServerVersion;
                var hasMot = sqlServerManagement.CurrentDatabase.HasMemoryOptimizedTableFileGroup;
                TestEnv.LogToArtifact("AddAction.log",
                    $"{ops,9:n1} 1/s | v{sqlVersion} on {TestEnv.TestConfigName} | {(hasMot ? "MOT" : "   ")} | {CrossInfo.ThePlatform} | {TestEnv.TestCpuName} | {providerName,-9} | {numThreads}T on {Environment.ProcessorCount} | {total} / {fail}"
                );
            }

        }
        class CounterHolder
        {
            public int Total;
        }


        IEnumerable<ActionDetailsWithCounters> GetSeedingBatch()
        {
            for (int j = 0; j <= 100; j++)
            {
                for (int i = 0; i < 26; i++)
                {
                    SqlInsightsActionKeyPath key = new SqlInsightsActionKeyPath(TestAppName, $"Section {(char) (i+65)}");
                    ActionDetailsWithCounters actionDetailsWithCounters = CreateActionDetailsWithCounters(key);
                    yield return actionDetailsWithCounters;
                }
            }
        }

        public static ActionDetailsWithCounters CreateActionDetailsWithCounters(SqlInsightsActionKeyPath key)
        {
            ActionDetailsWithCounters.SqlStatement stm = new ActionDetailsWithCounters.SqlStatement()
            {
                Counters = new SqlCounters() { Duration = 42 }
            };
            var actionDetailsWithCounters = new ActionDetailsWithCounters()
            {
                Key = key,
                At = DateTime.UtcNow,
                AppDuration = 2,
                AppName = "Tests",
                HostId = Environment.MachineName,
                IsOK = true,
                SqlStatements = { stm },
            };
            return actionDetailsWithCounters;
        }

    }
}