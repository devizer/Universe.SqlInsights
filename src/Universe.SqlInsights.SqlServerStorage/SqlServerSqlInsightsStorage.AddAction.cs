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

            string sqlMergeActionSummary = @$"If Exists(
  Select 1 From [SqlInsightsKeyPathSummary] {(isMemoryOptimized ? "" : "WITH (UPDLOCK,ROWLOCK)")}
  Where KeyPath = @KeyPath And HostId = @HostId And AppName = @AppName And IdSession = @IdSession)
Update [SqlInsightsKeyPathSummary] Set 
  Version = Cast(@@DBTS as BigInt),
  [Count] = [Count] + @Count,
  ErrorsCount = ErrorsCount + @ErrorsCount,
  AppDuration = AppDuration + @AppDuration,
  AppKernelUsage = AppKernelUsage + @AppKernelUsage,
  AppUserUsage = AppUserUsage + @AppUserUsage,
  SqlDuration = SqlDuration + @SqlDuration,
  SqlCPU = SqlCPU + @SqlCPU,
  SqlReads = SqlReads + @SqlReads,
  SqlWrites = SqlWrites + @SqlWrites,
  SqlRowCounts = SqlRowCounts + @SqlRowCounts,
  SqlRequests = SqlRequests + @SqlRequests,
  SqlErrors = SqlErrors + @SqlErrors
Where KeyPath = @KeyPath And HostId = @HostId And AppName = @AppName And IdSession = @IdSession;
Else
Insert Into [SqlInsightsKeyPathSummary]
(KeyPath, IdSession, AppName, HostId, Version, [Count], ErrorsCount, AppDuration, AppKernelUsage, AppUserUsage, SqlDuration, SqlCPU, SqlReads, SqlWrites, SqlRowCounts, SqlRequests, SqlErrors)
Values(@KeyPath, @IdSession, @AppName, @HostId, Cast(@@DBTS as BigInt), @Count, @ErrorsCount, @AppDuration, @AppKernelUsage, @AppUserUsage, @SqlDuration, @SqlCPU, @SqlReads, @SqlWrites, @SqlRowCounts, @SqlRequests, @SqlErrors);
";


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
                // long nextVersion = GetNextVersion(con, transaction: null);
                StringsStorage stringStorage = new StringsStorage(con, transaction: null);
                var idAppName = stringStorage.AcquireString(StringKind.AppName, reqAction.AppName);
                var idHostId = stringStorage.AcquireString(StringKind.HostId, reqAction.HostId);

                foreach (var idSession in aliveSessions)
                {

                    var keyPath = SerializeKeyPath(reqAction.Key);

                    // Either ReadCommitted or ReadUncommitted without MOT. Doesn't matter without deletion.
                    
                    // IDbTransaction tran = con.BeginTransaction(IsolationLevel.ReadUncommitted);
                    IDbTransaction tran = null; // for legacy
                    // if (isMemoryOptimized) tran = con.BeginTransaction(IsolationLevel.ReadUncommitted); SLOWER 
                    using (tran)
                    {
                        // SUMMARY: SqlInsightsKeyPathSummary
                        ActionSummaryCounters actionActionSummary = reqAction.AsSummary();
                        try
                        {
                            // TODO (without ReadCommitted only):
                            // System.Data.SqlClient.SqlException (0x80131904): Violation of PRIMARY KEY constraint 'PK_SqlInsightsKeyPathSummary'. Cannot insert duplicate key in object 'dbo.SqlInsightsKeyPathSummary'. The duplicate key value is (ASP.NET Core→SqlInsights→Summary→[POST], 0, 1, 3).
                            var query = con
                                .Execute(sqlMergeActionSummary, new
                                {
                                    IdSession = idSession,
                                    KeyPath = keyPath,
                                    AppName = idAppName,
                                    HostId = idHostId,
                                    Count = actionActionSummary.Count,
                                    ErrorsCount = actionActionSummary.RequestErrors,
                                    AppDuration = Math.Round(actionActionSummary.AppDuration, 6),
                                    AppKernelUsage = Math.Round(actionActionSummary.AppKernelUsage, 6),
                                    AppUserUsage = Math.Round(actionActionSummary.AppUserUsage, 6),
                                    SqlDuration = actionActionSummary.SqlCounters.Duration,
                                    SqlCPU = actionActionSummary.SqlCounters.CPU,
                                    SqlReads = actionActionSummary.SqlCounters.Reads,
                                    SqlWrites = actionActionSummary.SqlCounters.Writes,
                                    SqlRowCounts = actionActionSummary.SqlCounters.RowCounts,
                                    SqlRequests = actionActionSummary.SqlCounters.Requests,
                                    SqlErrors = actionActionSummary.SqlErrors,
                                }, tran);

                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException($"Unable to merge action summary on SqlInsightsKeyPathSummary", ex);
                        }

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

                        tran?.Commit();
                    }
                }

            }
        }


        [Obsolete("Inlined", true)]
        private static long GetNextVersion(IDbConnection con, IDbTransaction transaction)
        {
            var rowVersion = con.ExecuteScalar<byte[]>("Select @@DBTS;");
            return (long)RowVersion2Int64(rowVersion);
        }

    }
}