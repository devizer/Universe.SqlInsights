using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage
{
    public partial class SqlServerSqlInsightsStorage 
    {
#if NETSTANDARD || NET5_0 || NET461
        
        class SelectIdActionResult
        {
            public long IdAction { get; set; }
        }

        
        // TODO: Propagate parameters optionalApps, optionalHosts
        // Same as GetActionsByKeyPath below
        public async Task<string> GetKeyPathTimestampOfDetails(long idSession, SqlInsightsActionKeyPath keyPath, IEnumerable<string> optionalApps = null, IEnumerable<string> optionalHosts = null)
        {
            const string sql = "Select Top 1 IdAction From SqlInsightsAction Where KeyPath = @KeyPath And IdSession = @IdSession Order By IdAction Desc";
            using (var con = GetConnection())
            {
                var query = await con.QueryAsync<SelectIdActionResult>(sql, new {KeyPath = SerializeKeyPath(keyPath), IdSession = idSession});
                long? ret = query.FirstOrDefault()?.IdAction;
                return ret.HasValue ? ret.Value.ToString() : "";
            }
        }

        public async Task<IEnumerable<ActionDetailsWithCounters>> GetActionsByKeyPath(long idSession, SqlInsightsActionKeyPath keyPath, int lastN = 100, IEnumerable<string> optionalApps = null, IEnumerable<string> optionalHosts = null, bool? isOk = null)
        {
            if (lastN < 1) 
                throw new ArgumentOutOfRangeException(nameof(lastN));
            
            using (var con = GetConnection())
            {
                StringsStorage strings = new StringsStorage(con, null);
                var optionalParams = BuildOptionalParameters(strings, optionalApps, optionalHosts);
                var sqlParams = optionalParams.Parameters;
                var sqlWhere = optionalParams.SqlWhere;

                string sqlWhereIsOk = isOk == null ? "" : isOk == true ? " And IsOK = (1)" : " And IsOK = (0)";
                string sql = $"Select Top (@N) Data From SqlInsightsAction Where KeyPath = @KeyPath And IdSession = @IdSession{sqlWhereIsOk}{sqlWhere} Order By IdAction Desc";
                
                sqlParams.Add("KeyPath", SerializeKeyPath(keyPath));
                sqlParams.Add("IdSession", idSession);
                sqlParams.Add("N", lastN);
                
                var query = await con
                    .QueryAsync<SelectDataResult>(sql, sqlParams);

                var ret = query
                    .Select(x => DbJsonConvert.Deserialize<ActionDetailsWithCounters>(x.Data));

                return ret.ToList();
            }
        }

#endif        
    }
}