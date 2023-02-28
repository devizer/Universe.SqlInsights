﻿using System.Collections.Generic;
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
    IdSession = @IdSession");
            
            using (var con = GetConnection())
            {
                StringsStorage strings = new StringsStorage(con, null);
                var optionalParams = BuildOptionalParameterts(strings, optionalApp, optionalHost);
                var sqlParams = optionalParams.Parameters;
                sqlParams.Add("IdSession", idSession);
                sql.Append(optionalParams.SqlWhere);
                
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
                        var deserialized = DbJsonConvert.Deserialize<ActionSummaryCounters>(raw);
                        key = deserialized.Key;
                        next.Add(deserialized);
                    }

                    next.Key = key;
                    ret.Add(next);
                }

                return ret;
                
                var query = resultSet.Select(x =>
                {
                    return DbJsonConvert.Deserialize<ActionSummaryCounters>(x.Data);
                });

                return query.ToList();
            }
        }

        public async Task<string> GetActionsSummaryTimestamp(long idSession, string optionalApp = null, string optionalHost = null)
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
                var optionalParams = BuildOptionalParameterts(strings, optionalApp, optionalHost);
                var sqlParams = optionalParams.Parameters;
                sqlParams.Add("IdSession", idSession);
                sql.Append(optionalParams.SqlWhere);
                sqlParams.Add("IdSession", idSession);
                
                var query = await con.QueryAsync<long?>(sql.ToString(), sqlParams);
                long? binaryVersion = query.FirstOrDefault();
                return binaryVersion.ToString();
            }
        }

#endif        
    }
}