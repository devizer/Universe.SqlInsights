using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
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
            SqlServerSqlInsightsStorage.ResetCacheForTests();
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
            
            var cnn = this.ProviderFactory.CreateConnection();
            cnn.ConnectionString = this.ConnectionString;

            Func<int?> GetTotalActionRecords = () => cnn.QueryFirstOrDefault<int?>("Select Count(1) From SqlInsightsAction");

            int? totalActionRecordsBefore = GetTotalActionRecords();
            
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
                    // string err = (sqlError == null ? "" : $"#{sqlError.Number} ") + ex.Message;
                    string err = ex.GetExceptionDigest();
                    var counter = errors.GetOrAdd(err, errorKey => new CounterHolder());
                    counter.Total += 1;
                }
            });

            int? totalActionRecordsAfter = GetTotalActionRecords();
            var totalActionRecordsDelta = totalActionRecordsAfter.GetValueOrDefault() - totalActionRecordsBefore.GetValueOrDefault();

            var ops = (double) total / sw.Elapsed.TotalSeconds;
            var actualCores = Math.Min(Environment.ProcessorCount, numThreads);
            var actualOps = (double)(total - fail) / sw.Elapsed.TotalSeconds / actualCores;
            var providerName = ProviderFactory.GetShortProviderName();
            var errorsInfo = errors.Count == 0 ? "" : $"{Environment.NewLine}{string.Join(Environment.NewLine, errors.OrderByDescending(x => x.Value.Total).Select(x => $"N={x.Value.Total}: {x.Key}"))}";
            Console.WriteLine($"[{providerName}] {totalActionRecordsDelta}; OPS = {actualOps:n1} * {actualCores}T actions per second. Adding Count: {total}. Fail count: {fail} (Cores: {Environment.ProcessorCount}, Threads: {numThreads}){errorsInfo}");
            if (total > 129)
            {
                var sqlServerManagement = cnn.Manage();
                var sqlVersion = sqlServerManagement.ShortServerVersion;
                var hasMot = sqlServerManagement.CurrentDatabase.HasMemoryOptimizedTableFileGroup;
                // var hasMot = cnn.Query<string>("Select Top 1 name from sys.filegroups where type = 'FX'").FirstOrDefault() != null;
                var testConfiguration = string.IsNullOrEmpty(TestEnv.TestConfigName) ? "" : $" on {TestEnv.TestConfigName}";
                TestEnv.LogToArtifact("AddAction.log",
                    $"{actualOps,9:n1} 1/s * {actualCores}T | v{sqlVersion}{testConfiguration} | {(hasMot ? "MOT" : "   ")} | {CrossInfo.ThePlatform} | {TestEnv.TestCpuName} | {providerName,-9} | {numThreads}T on {Environment.ProcessorCount} | {total} / {fail}"
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