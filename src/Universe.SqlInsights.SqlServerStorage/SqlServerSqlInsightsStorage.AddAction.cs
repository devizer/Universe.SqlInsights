using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage
{
    public partial class SqlServerSqlInsightsStorage 
    {
        public void AddAction(ActionDetailsWithCounters reqAction)
        {

            if (reqAction.AppName == null) throw new ArgumentException("Missing reqAction.AppName");

            const string sqlNextVersion = @"
Update SqlInsightsKeyPathSummaryTimestamp Set Guid = NewId(), Version = Version + 1;
Select Top 1 Version From SqlInsightsKeyPathSummaryTimestamp;
";

            const string
                sqlSelect = "Select Data From SqlInsightsKeyPathSummary WITH (UPDLOCK) Where KeyPath = @KeyPath And HostId = @HostId And AppName = @AppName And IdSession = @IdSession",
                sqlInsert = "Insert SqlInsightsKeyPathSummary(KeyPath, IdSession, AppName, HostId, Data, Version) Values(@KeyPath, @IdSession, @AppName, @HostId, @Data, @Version);",
                sqlUpdate = "Update SqlInsightsKeyPathSummary Set Data = @Data, Version = @Version Where KeyPath = @KeyPath And HostId = @HostId And AppName = @AppName And IdSession = @IdSession";

            var aliveSessions = GetAliveSessions().ToList();
#if DEBUG            
            Console.WriteLine($"[AddAction] Alive Sessions {string.Join(",", aliveSessions.Select(x => x.ToString()).ToArray())}");
#endif            
            if (aliveSessions.Count <= 0) return;
            
            using (IDbConnection con = GetConnection())
            {
                var tran = con.BeginTransaction(IsolationLevel.ReadCommitted);
                using (tran)
                {
                    IEnumerable<long> nextVersionQuery = con.Query<long>(sqlNextVersion, null, tran);
                    var nextVersion = nextVersionQuery.FirstOrDefault() + 1; 
                    foreach (var idSession in aliveSessions)
                    {
                        StringsStorage stringStorage = new StringsStorage(con, tran);
                        var idAppName = stringStorage.AcquireString(StringKind.AppName, reqAction.AppName);
                        var idHostId = stringStorage.AcquireString(StringKind.HostId, reqAction.HostId);

                        var keyPath = SerializeKeyPath(reqAction.Key);

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

                        string rawDataPrev = query
                            .FirstOrDefault()?
                            .Data;

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
                        try
                        {
                            con.Execute(sqlUpsert, new
                            {
                                KeyPath = keyPath,
                                IdSession = idSession,
                                Data = dataSummary,
                                AppName = idAppName,
                                HostId = idHostId,
                                Version = nextVersion,
                            }, tran);
                        }
                        catch (Exception ex)
                        {
                            var exx = ex.ToString();
                            throw;
                        }

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
                    }
                    
                    tran.Commit();
                }
            }
        }
    }
}