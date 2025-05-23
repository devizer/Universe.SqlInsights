﻿using System;
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
  AppDurationSquared = AppDurationSquared + (@AppDuration * @AppDuration),
  AppKernelUsage = AppKernelUsage + @AppKernelUsage,
  AppKernelUsageSquared = AppKernelUsageSquared + (@AppKernelUsage * @AppKernelUsage),
  AppUserUsage = AppUserUsage + @AppUserUsage,
  AppUserUsageSquared = AppUserUsageSquared + (@AppUserUsage * @AppUserUsage),
  SqlDuration = SqlDuration + @SqlDuration,
  SqlDurationSquared = SqlDurationSquared + (@SqlDuration * @SqlDuration),
  SqlCPU = SqlCPU + @SqlCPU,
  SqlCPUSquared = SqlCPUSquared + (@SqlCPU * @SqlCPU),
  SqlReads = SqlReads + @SqlReads,
  SqlReadsSquared = SqlReadsSquared + (@SqlReads * @SqlReads),
  SqlWrites = SqlWrites + @SqlWrites,
  SqlWritesSquared = SqlWritesSquared + (@SqlWrites * @SqlWrites),
  SqlRowCounts = SqlRowCounts + @SqlRowCounts,
  SqlRowCountsSquared = SqlRowCountsSquared + (@SqlRowCounts + @SqlRowCounts),
  SqlRequests = SqlRequests + @SqlRequests,
  SqlRequestsSquared = SqlRequestsSquared + (@SqlRequests * @SqlRequests),
  SqlErrors = SqlErrors + @SqlErrors,
  SqlErrorsSquared = SqlErrorsSquared + (@SqlErrors * @SqlErrors)
Where KeyPath = @KeyPath And HostId = @HostId And AppName = @AppName And IdSession = @IdSession;
Else
Insert Into [SqlInsightsKeyPathSummary]
(KeyPath, IdSession, AppName, HostId, Version, [Count], ErrorsCount, AppDuration, AppDurationSquared, AppKernelUsage, AppKernelUsageSquared, AppUserUsage, AppUserUsageSquared, SqlDuration, SqlDurationSquared, SqlCPU, SqlCPUSquared, SqlReads, SqlReadsSquared, SqlWrites, SqlWritesSquared, SqlRowCounts, SqlRowCountsSquared, SqlRequests, SqlRequestsSquared, SqlErrors, SqlErrorsSquared)
Values(@KeyPath, @IdSession, @AppName, @HostId, Cast(@@DBTS as BigInt), @Count, @ErrorsCount, @AppDuration, (@AppDuration * @AppDuration), @AppKernelUsage, (@AppKernelUsage * @AppKernelUsage), @AppUserUsage, (@AppUserUsage * @AppUserUsage), @SqlDuration, (@SqlDuration * @SqlDuration), @SqlCPU, (@SqlCPU * @SqlCPU), @SqlReads, (@SqlReads * @SqlReads), @SqlWrites, (@SqlWrites * @SqlWrites), @SqlRowCounts, (@SqlRowCounts * @SqlRowCounts), @SqlRequests, (@SqlRequests * @SqlRequests), @SqlErrors, (@SqlErrors * @SqlErrors));
";


            
            List<long> aliveSessions;
            try
            {
                aliveSessions = GetAliveSessions().ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Sql Insights] Storage does not respond for GetAliveSessions()");
                throw;
            }

            if (DebugAddAction)
            {
                double msec = DebuggerStopwatch.ElapsedTicks * 1000d / Stopwatch.Frequency;
                var aliveSessionsInfo = string.Join(",", aliveSessions.Select(x => x.ToString()).ToArray());
                Console.WriteLine($"{Counter,6} [AddAction] Alive Sessions >{aliveSessionsInfo}< \"{reqAction.Key}\" (took {msec:n2} msec)");
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
                        const string sqlInsertDetail = @"Insert SqlInsightsAction(At, IdSession, KeyPath, IsOK, AppName, HostId, AppDuration, AppKernelUsage, AppUserUsage, SqlDuration, SqlCPU, SqlReads, SqlWrites, SqlRowCounts, SqlRequests, SqlErrors, Data)
Values(@At, @IdSession, @KeyPath, @IsOK, @AppName, @HostId, @AppDuration, @AppKernelUsage, @AppUserUsage, @SqlDuration, @SqlCPU, @SqlReads, @SqlWrites, @SqlRowCounts, @SqlRequests, @SqlErrors, @Data)";

                        var detail = reqAction;
                        var dataDetail = DbJsonConvert.Serialize(detail);

                        long GetIntSum(Func<ActionDetailsWithCounters.SqlStatement,long> getItem)
                        {
                            return detail.SqlStatements.Count == 0 ? 0 : detail.SqlStatements.Sum(getItem);
                        }

                        try
                        {

                            con.Execute(sqlInsertDetail, new
                            {
                                At = detail.At,
                                IsOK = string.IsNullOrEmpty(detail.BriefException),
                                IdSession = idSession,
                                KeyPath = keyPath,
                                AppDuration = detail.AppDuration,
                                AppKernelUsage = detail.AppKernelUsage,
                                AppUserUsage = detail.AppUserUsage,
                                SqlDuration = GetIntSum(x => x.Counters.Duration),
                                SqlCPU = GetIntSum(x => x.Counters.CPU),
                                SqlReads = GetIntSum(x => x.Counters.Reads),
                                SqlWrites = GetIntSum(x => x.Counters.Writes),
                                SqlRowCounts = GetIntSum(x => x.Counters.RowCounts),
                                SqlRequests = GetIntSum(x => 1),
                                SqlErrors = GetIntSum(x => x.SqlErrorCode.HasValue ? 1 : 0),
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