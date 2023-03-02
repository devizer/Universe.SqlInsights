using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Dapper;
using Newtonsoft.Json;
using Universe.SqlInsights.Shared;
using Universe.SqlServerJam;

namespace Universe.SqlInsights.SqlServerStorage
{
    public partial class SqlServerSqlInsightsStorage
    {
        private static long CounterStorage;
        private long Counter;
        Stopwatch DebuggerStopwatch = Stopwatch.StartNew();

        public static bool DebugAddAction = true;

        public void AddAction(ActionDetailsWithCounters reqAction)
        {

            if (reqAction.AppName == null) throw new ArgumentException("Missing reqAction.AppName");

            bool isMemoryOptimized = MetadataCache.IsMemoryOptimized(ConnectionString);

            string
                sqlSelect =
                    $"Select Data From SqlInsightsKeyPathSummary {(isMemoryOptimized ? "" : "WITH (UPDLOCK,ROWLOCK)")} Where KeyPath = @KeyPath And HostId = @HostId And AppName = @AppName And IdSession = @IdSession";
            
            const string    
                sqlInsert =
                    "Insert SqlInsightsKeyPathSummary(KeyPath, IdSession, AppName, HostId, Data, Version) Values(@KeyPath, @IdSession, @AppName, @HostId, @Data, @Version);",
                sqlUpdate =
                    "Update SqlInsightsKeyPathSummary Set Data = @Data, Version = @Version Where KeyPath = @KeyPath And HostId = @HostId And AppName = @AppName And IdSession = @IdSession";

            var aliveSessions = GetAliveSessions().ToList();

            if (DebugAddAction)
            {
                double msec = DebuggerStopwatch.ElapsedTicks * 1000d / Stopwatch.Frequency;
                var aliveSessionsInfo = string.Join(",", aliveSessions.Select(x => x.ToString()).ToArray());
                Console.WriteLine($"{msec,15:n2} {Counter,-4} [AddAction] Alive Sessions >{aliveSessionsInfo}< \"{reqAction.Key}\"");
            }

            if (aliveSessions.Count <= 0) return;

            using (IDbConnection con = GetConnection())
            {
                var nextVersion = GetNextVersion(con, transaction: null);
                StringsStorage stringStorage = new StringsStorage(con, transaction: null);
                var idAppName = stringStorage.AcquireString(StringKind.AppName, reqAction.AppName);
                var idHostId = stringStorage.AcquireString(StringKind.HostId, reqAction.HostId);

                foreach (var idSession in aliveSessions)
                {

                    var keyPath = SerializeKeyPath(reqAction.Key);

                    // Either ReadCommitted or ReadUncommitted without MOT. Doesn't matter without deletion.
                    
                    // IDbTransaction tran = con.BeginTransaction(IsolationLevel.ReadUncommitted);
                    IDbTransaction tran = null;
                    using (tran)
                    {
                        // SUMMARY: SqlInsightsKeyPathSummary
                        ActionSummaryCounters actionActionSummary = reqAction.AsSummary();
                        var query = con
                            .Query<SelectDataResult>(sqlSelect, new
                            {
                                IdSession = idSession,
                                KeyPath = keyPath,
                                AppName = idAppName,
                                HostId = idHostId,
                            }, tran);

                        string rawDataPrev = query.FirstOrDefault()?.Data;

                        bool exists = rawDataPrev != null;
                        ActionSummaryCounters
                            next,
                            prev = !exists
                                ? new ActionSummaryCounters()
                                : DbJsonConvert.Deserialize<ActionSummaryCounters>(rawDataPrev);

                        if (exists)
                        {
                            prev.Add(actionActionSummary);
                            next = prev;
                        }
                        else
                        {
                            next = actionActionSummary;
                        }

                        // next.Key = actionActionSummary.Key;
                        var sqlUpsert = exists ? sqlUpdate : sqlInsert;
                        var dataSummary = DbJsonConvert.Serialize(next);
                        // TODO (without ReadCommitted only):
                        // System.Data.SqlClient.SqlException (0x80131904): Violation of PRIMARY KEY constraint 'PK_SqlInsightsKeyPathSummary'. Cannot insert duplicate key in object 'dbo.SqlInsightsKeyPathSummary'. The duplicate key value is (ASP.NET Core→SqlInsights→Summary→[POST], 0, 1, 3).
                        con.Execute(sqlUpsert, new
                        {
                            KeyPath = keyPath,
                            IdSession = idSession,
                            Data = dataSummary,
                            AppName = idAppName,
                            HostId = idHostId,
                            Version = nextVersion,
                        }, tran);

                        // DETAILS: SqlInsightsAction
                        const string sqlInsertDetail = @"Insert SqlInsightsAction(At, IdSession, KeyPath, IsOK, AppName, HostId, Data)
Values(@At, @IdSession, @KeyPath, @IsOK, @AppName, @HostId, @Data)";

                        var detail = reqAction;
                        var dataDetail = DbJsonConvert.Serialize(detail);
                        try
                        {
                            con.Execute(sqlInsertDetail, new
                            {
                                At = detail.At,
                                IsOK = string.IsNullOrEmpty(detail.BriefException),
                                IdSession = idSession,
                                KeyPath = keyPath,
                                Data = dataDetail,
                                AppName = idAppName,
                                HostId = idHostId,
                            }, tran);
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }

                        // tran.Commit();
                    }
                }

            }
        }


        private static int TotalNextVersion, FailNextVersion;
        // TODO: If deadlock retry again
        private static long GetNextVersion(IDbConnection con, IDbTransaction transaction)
        {
            /*
            const string sqlNextVersion = @"
Update Top (1) SqlInsightsKeyPathSummaryTimestamp Set Guid = NewId(), Version = Version + 1;
Select Top 1 Version From SqlInsightsKeyPathSummaryTimestamp;
";
*/
            const string sqlNextVersion = @"Update Top (1) SqlInsightsKeyPathSummaryTimestamp Set Version = Version + 1 Output inserted.Version;";

            long nextVersion = -1;
            bool isDeadLock = false;
            Exception nextVersionQueryError = null;
            Stopwatch startNextVersion = Stopwatch.StartNew();
            int total = Interlocked.Increment(ref TotalNextVersion), fail = FailNextVersion;
            try
            {
                IEnumerable<long> nextVersionQuery = con.Query<long>(sqlNextVersion, null, transaction);
                nextVersion = nextVersionQuery.FirstOrDefault() + 1;
                // Console.WriteLine($"WTFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF. nextVersion='{nextVersion}'");
            }
            catch (Exception ex)
            {
                fail = Interlocked.Increment(ref FailNextVersion);
                nextVersionQueryError = ex;
                isDeadLock = ex.FindSqlError()?.Number == 1205;
                // Console.WriteLine($"FAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIL. {ex.GetExeptionDigest()}");
            }

            if (DebugAddAction)
            {
                var msecNextVersion = startNextVersion.ElapsedTicks * 1000d / Stopwatch.Frequency;
                string errorDetails = null;
                if (nextVersionQueryError != null)
                {
                     errorDetails = $"[{nextVersionQueryError.GetType()}] '{nextVersionQueryError.Message}'";
                    var sqlError = SqlExceptionExtensions.IsSqlException(nextVersionQueryError);
                    if (sqlError != null) errorDetails = $"[{nextVersionQueryError.GetType()} N{sqlError.Number}] '{nextVersionQueryError.Message}'"; 
                }
                
                Console.WriteLine(
                    $"[NextVersionQuery {fail}/{total}] {msecNextVersion:n2} IsDeadlock: {(!isDeadLock ? "no" : "--<=DEADLOCK=>--")}{(nextVersionQueryError == null ? null : $" {errorDetails}")}");
            }

            if (nextVersionQueryError != null)
                throw new InvalidOperationException("Unable to create next version", nextVersionQueryError);

            return nextVersion;
        }
    }
}