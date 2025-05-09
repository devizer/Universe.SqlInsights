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
#if NETSTANDARD || NET5_0 || NET461 || NET5_0_OR_GREATER

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

        // keyPath == null: Return all actions ordered Ascending
        public async Task<IEnumerable<ActionDetailsWithCounters>> GetActionsByKeyPath(long idSession, SqlInsightsActionKeyPath keyPath, int lastN = 100, IEnumerable<string> optionalApps = null, IEnumerable<string> optionalHosts = null, bool? isOk = null)
        {
            if (lastN < 1) 
                throw new ArgumentOutOfRangeException(nameof(lastN));

            var con = GetConnection();
            // using (var con = GetConnection()) NON-BUFFERED. 
            {
                StringsStorage strings = new StringsStorage(con, null);
                var optionalParams = BuildOptionalParameters(strings, optionalApps, optionalHosts);
                var sqlParams = optionalParams.Parameters;
                var sqlWhere = optionalParams.SqlWhere;

                // Null KeyPath: Opposite Order for Export/Import
                string sqlWhereIsOk = isOk == null ? "" : isOk == true ? " And IsOK = (1)" : " And IsOK = (0)";
                string sqlWhereKeyPath = keyPath == null ? "" : "KeyPath = @KeyPath And ";
                string sqlOrderBy = keyPath == null ? "IdAction Asc" : "IdAction Desc";
                string sql = $"Select Top (@N) Data From SqlInsightsAction With (NoLock) Where {sqlWhereKeyPath}IdSession = @IdSession{sqlWhereIsOk}{sqlWhere} Order By {sqlOrderBy}";
                
                if (keyPath != null) sqlParams.Add("KeyPath", SerializeKeyPath(keyPath));
                sqlParams.Add("IdSession", idSession);
                sqlParams.Add("N", lastN);

                // non buffered throw exception
                CommandFlags bufferMode = keyPath == null ? CommandFlags.None : CommandFlags.Buffered;
                CommandDefinition cmd = new CommandDefinition(sql, sqlParams, flags: bufferMode);
                // Console.WriteLine($"[DEBUG ConnectionString] GET ACTIONS {con.ConnectionString}");
                var query = con.Query<SelectDataResult>(cmd);
                // var query = con.Query<SelectDataResult>(/*cmd*/sql, sqlParams, buffered: true);


                if (keyPath != null) query = query.ToList();
                var ret = query
                    .Select(x => DbJsonConvert.Deserialize<ActionDetailsWithCounters>(x.Data));

                // return ret.ToList();
                // if (keyPath != null) con.Close();
                return ret;
            }
        }

#endif        
    }
}