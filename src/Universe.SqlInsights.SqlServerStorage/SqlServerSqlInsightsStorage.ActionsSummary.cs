using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage
{
    partial class SqlServerSqlInsightsStorage 
    {
#if NETSTANDARD
        
        class SelectKeyAndDataResult
        {
            public string KeyPath { get; set; }
            public string Data { get; set; }
        }

        public async Task<IEnumerable<ActionSummaryCounters>> GetActionsSummary(long idSession, string optionalApp = null, string optionalHost = null)
        {
            StringBuilder sql = new StringBuilder(@"
Select 
    KeyPath, 
    Data 
From 
    SqlInsightsKeyPathSummary 
Where 
    IdSession = @IdSession
");
            
            using (var con = GetConnection())
            {
                StringsStorage strings = new StringsStorage(con, null);
                var sqlParams = new DynamicParameters();
                sqlParams.Add("IdSession", idSession);
                if (optionalApp != null)
                {
                    long? idAppName = strings.AcquireString(StringKind.AppName, optionalApp);
                    sqlParams.Add("AppName", idAppName.Value);
                    sql.Append(" And AppName = @AppName");
                }
                if (optionalHost != null)
                {
                    long? idHost = strings.AcquireString(StringKind.HostId, optionalHost);
                    sqlParams.Add("HostId", idHost.Value);
                    sql.Append(" And HostId = @HostId");
                }
                
                IEnumerable<SelectKeyAndDataResult> resultSet = await con.QueryAsync<SelectKeyAndDataResult>(sql.ToString(), sqlParams);

                // Ok: Group By x.KeyPath and sum all
                var groups = resultSet.GroupBy(x => x.KeyPath, x => x.Data);
                List<ActionSummaryCounters> ret = new List<ActionSummaryCounters>();
                foreach (var src in groups)
                {
                    ActionSummaryCounters next = new ActionSummaryCounters();
                    SqlInsightsActionKeyPath key = null;
                    foreach (var raw in src)
                    {
                        var deserialized = JsonConvert.DeserializeObject<ActionSummaryCounters>(raw, DefaultSettings);
                        key = deserialized.Key;
                        next.Add(deserialized);
                    }

                    next.Key = key;
                    ret.Add(next);
                }

                return ret;
                
                var query = resultSet.Select(x =>
                {
                    return JsonConvert.DeserializeObject<ActionSummaryCounters>(x.Data, DefaultSettings);
                });

                return query.ToList();
            }
        }

        public async Task<string> GetActionsSummaryTimestamp(long idSession, string optionalApp = null, string optionalHost = null)
        {
            StringBuilder sqlWhere = new StringBuilder();
            using (var con = GetConnection())
            {
                StringsStorage strings = new StringsStorage(con, null);
                var sqlParams = new DynamicParameters();
                sqlParams.Add("IdSession", idSession);
                if (optionalApp != null)
                {
                    long? idAppName = strings.AcquireString(StringKind.AppName, optionalApp);
                    sqlParams.Add("AppName", idAppName.Value);
                    sqlWhere.Append(" And AppName = @AppName");
                }
                if (optionalHost != null)
                {
                    long? idHost = strings.AcquireString(StringKind.HostId, optionalHost);
                    sqlParams.Add("HostId", idHost.Value);
                    sqlWhere.Append(" And HostId = @HostId");
                }

                var sql = @$"
Select 
    Max(Version) 
From 
    SqlInsightsKeyPathSummary 
Where 
    IdSession = @IdSession {sqlWhere}";
                
                var query = await con.QueryAsync<long?>(sql, sqlParams);
                long? binaryVersion = query.FirstOrDefault();
                return binaryVersion.ToString();
            }
        }

#endif        
    }
}