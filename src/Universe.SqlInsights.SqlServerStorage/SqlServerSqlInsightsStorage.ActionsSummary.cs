using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage
{
    // W3API Only
    partial class SqlServerSqlInsightsStorage 
    {
#if NETSTANDARD || NET5_0 || NET461 || NET5_0_OR_GREATER

        class SelectKeyAndDataResult
        {
            public string KeyPath { get; set; }
            public string Data { get; set; }
        }

        public async Task<IEnumerable<ActionSummaryCounters>> GetActionsSummary(long idSession, IEnumerable<string> optionalApps = null, IEnumerable<string> optionalHosts = null)
        {
            StringBuilder sql = new StringBuilder(@"
Select
    KeyPath,
    [Count],
    ErrorsCount,
    AppDuration,
    AppKernelUsage,
    AppUserUsage,
    SqlDuration,
    SqlCPU,
    SqlReads,
    SqlWrites,
    SqlRowCounts,
    SqlRequests,
    SqlErrors
From 
    SqlInsightsKeyPathSummary 
Where 
    IdSession = @IdSession"); // And/Or AppName equals, HostId equals args
            
            using (var con = GetConnection())
            {
                StringsStorage strings = new StringsStorage(con, null);
                var optionalParams = BuildOptionalParameters(strings, optionalApps, optionalHosts);
                var sqlParams = optionalParams.Parameters;
                sqlParams.Add("IdSession", idSession);
                sql.Append(optionalParams.SqlWhere);
                
                IEnumerable<SummaryDataRow> resultSet = await con.QueryAsync<SummaryDataRow>(sql.ToString(), sqlParams);

                // Ok: Group By x.KeyPath and sum all
                var groups = resultSet.GroupBy(x => x.KeyPath, x => x);
                List<ActionSummaryCounters> ret = new List<ActionSummaryCounters>();
                foreach (var src in groups)
                {
                    
                    ActionSummaryCounters next = new ActionSummaryCounters();
                    SqlInsightsActionKeyPath key = null;
                    foreach (var raw in src)
                    {
                        var deserialized = raw.ToActionSummaryCounters();
                        key = deserialized.Key;
                        next.Add(deserialized);
                    }

                    next.Key = key;
                    ret.Add(next);
                }

                return ret;
            }
        }

        // Done: Remove Data Json
        public async Task<string> GetActionsSummaryTimestamp(long idSession, IEnumerable<string> optionalApps = null, IEnumerable<string> optionalHosts = null)
        {
            var sql = new StringBuilder(@$"
Select 
    Max(Version) 
From 
    SqlInsightsKeyPathSummary 
Where 
    IdSession = @IdSession");

            using (var con = GetConnection())
            {
                StringsStorage strings = new StringsStorage(con, null);
                OptionalParametersInfo optionalParams = BuildOptionalParameters(strings, optionalApps, optionalHosts);
                DynamicParameters sqlParams = optionalParams.Parameters;
                sqlParams.Add("IdSession", idSession);
                sql.Append(optionalParams.SqlWhere);
                
                var query = await con.QueryAsync<long?>(sql.ToString(), sqlParams);
                long? binaryVersion = query.FirstOrDefault();
                return binaryVersion.ToString();
            }
        }

#endif        
    }
}