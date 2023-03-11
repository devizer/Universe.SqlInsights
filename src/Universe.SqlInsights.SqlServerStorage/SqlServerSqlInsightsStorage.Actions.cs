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
#if NETSTANDARD || NET5_0
        
        class SelectIdActionResult
        {
            public long IdAction { get; set; }
        }

        
        public async Task<string> GetKeyPathTimestampOfDetails(long idSession, SqlInsightsActionKeyPath keyPath, string optionalApp = null, string optionalHost = null)
        {
            const string sql = "Select Top 1 IdAction From SqlInsightsAction Where KeyPath = @KeyPath And IdSession = @IdSession Order By IdAction Desc";
            using (var con = GetConnection())
            {
                var query = await con.QueryAsync<SelectIdActionResult>(sql, new {KeyPath = SerializeKeyPath(keyPath), IdSession = idSession});
                long? ret = query.FirstOrDefault()?.IdAction;
                return ret.HasValue ? ret.Value.ToString() : "";
            }
        }

        public async Task<IEnumerable<ActionDetailsWithCounters>> GetActionsByKeyPath(long idSession, SqlInsightsActionKeyPath keyPath, int lastN = 100, string optionalApp = null, string optionalHost = null)
        {
            if (lastN < 1) 
                throw new ArgumentOutOfRangeException(nameof(lastN));
            
            const string sql = "Select Top (@N) Data From SqlInsightsAction Where KeyPath = @KeyPath And IdSession = @IdSession Order By IdAction Desc";
            using (var con = GetConnection())
            {
                var query = await con
                    .QueryAsync<SelectDataResult>(sql, new {KeyPath = SerializeKeyPath(keyPath), IdSession = idSession, N = lastN});

                var ret = query
                    .Select(x => DbJsonConvert.Deserialize<ActionDetailsWithCounters>(x.Data));

                return ret.ToList();
            }
        }

#endif        
    }
}