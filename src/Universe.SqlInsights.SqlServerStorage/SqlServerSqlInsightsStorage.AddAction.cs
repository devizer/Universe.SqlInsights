using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Dapper;
using Newtonsoft.Json;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage
{
    public partial class SqlServerSqlInsightsStorage
    {
        private static long CounterStorage;
        private long Counter;
        Stopwatch DebuggerStopwatch = Stopwatch.StartNew();

        public void AddAction(ActionDetailsWithCounters reqAction)
        {

            if (reqAction.AppName == null) throw new ArgumentException("Missing reqAction.AppName");

            const string
                sqlSelect =
                    "Select Data From SqlInsightsKeyPathSummary WITH (UPDLOCK) Where KeyPath = @KeyPath And HostId = @HostId And AppName = @AppName And IdSession = @IdSession",
                sqlInsert =
                    "Insert SqlInsightsKeyPathSummary(KeyPath, IdSession, AppName, HostId, Data, Version) Values(@KeyPath, @IdSession, @AppName, @HostId, @Data, @Version);",
                sqlUpdate =
                    "Update SqlInsightsKeyPathSummary Set Data = @Data, Version = @Version Where KeyPath = @KeyPath And HostId = @HostId And AppName = @AppName And IdSession = @IdSession";

            var aliveSessions = GetAliveSessions().ToList();
#if DEBUG
            double msec = DebuggerStopwatch.ElapsedTicks * 1000d / Stopwatch.Frequency;
            var aliveSessionsInfo = string.Join(",", aliveSessions.Select(x => x.ToString()).ToArray());
            Console.WriteLine($"{msec,15:n2} {Counter,-4} [AddAction] Alive Sessions >{aliveSessionsInfo}< \"{reqAction.Key}\"");
#endif
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

                    var tran = con.BeginTransaction(IsolationLevel.ReadUncommitted);
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
                                : JsonConvert.DeserializeObject<ActionSummaryCounters>(rawDataPrev, DefaultSettings);

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
                        var dataSummary = JsonConvert.SerializeObject(next, DefaultSettings);
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
                        var dataDetail = JsonConvert.SerializeObject(detail, DefaultSettings);
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

                        tran.Commit();
                    }
                }

            }
        }


        private static int TotalNextVersion, FailNextVersion;
        private static long GetNextVersion(IDbConnection con, IDbTransaction transaction)
        {
            const string sqlNextVersion = @"
Update SqlInsightsKeyPathSummaryTimestamp Set Guid = NewId(), Version = Version + 1;
Select Top 1 Version From SqlInsightsKeyPathSummaryTimestamp;
";

            long nextVersion = -1;
            bool isDeadLock = false;
            Exception nextVersionQueryError = null;
            Stopwatch startNextVersion = Stopwatch.StartNew();
            int total = Interlocked.Increment(ref TotalNextVersion), fail = FailNextVersion;
            try
            {
                IEnumerable<long> nextVersionQuery = con.Query<long>(sqlNextVersion, null, transaction);
                nextVersion = nextVersionQuery.FirstOrDefault() + 1;
            }
            catch (Exception ex)
            {
                fail = Interlocked.Increment(ref FailNextVersion);
                nextVersionQueryError = ex;
                if (ex is SqlException sqlException)
                {
                    isDeadLock = sqlException.Errors.OfType<SqlError>().Any(x => x.Number == 1205);
                    foreach (SqlError sqlExceptionError in sqlException.Errors)
                    {
                        if (sqlExceptionError.Number == 1205) isDeadLock = true;
                    }
                }
            }
#if DEBUG
            var msecNextVersion = startNextVersion.ElapsedTicks * 1000d / Stopwatch.Frequency;
            Console.WriteLine(
                $"[NextVersionQuery {fail}/{total}] {msecNextVersion:n2} IsDeadlock: {(!isDeadLock ? "no" : "--<=DEADLOCK=>--")}{(nextVersionQueryError == null ? null : $" [{nextVersionQueryError.GetType()}] '{nextVersionQueryError.Message}'")}");
            if (nextVersionQueryError != null)
                throw new InvalidOperationException("Unable to create next version", nextVersionQueryError);
#endif
            return nextVersion;
        }
    }
}