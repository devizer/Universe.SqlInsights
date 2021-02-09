using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage
{
    public class SqlServerSqlInsightsStorage : ISqlInsightsStorage
    {
        public readonly string ConnectionString;

        private static volatile bool AreMigrationsChecked = false;
        private static readonly object SyncMigrations = new object();

        public SqlServerSqlInsightsStorage(string connectionString)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private static readonly DefaultContractResolver TheContractResolver = new DefaultContractResolver
        {
        };

        private static JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ContractResolver = TheContractResolver,
            /*Converters = new JsonConverterCollection(),*/
        };

        IDbConnection GetConnection()
        {
            // TODO: Multiple Storages per app?
            if (!AreMigrationsChecked)
                lock(SyncMigrations)
                    if (!AreMigrationsChecked)
                    {
                        new SqlServerSqlInsightsMigrations(ConnectionString).Migrate();
                        AreMigrationsChecked = true;
                    }

            var ret = new SqlConnection(ConnectionString);
            ret.Open();
            return ret;
        }

        class SelectDataResult
        {
            public string Data { get; set; }
        }

        public void AddAction(ActionDetailsWithCounters reqAction)
        {

            const string
                sqlSelect = "Select Data From SqlInsightsKeyPathSummary Where KeyPath = @KeyPath And IdSession = @IdSession",
                sqlInsert = "Insert SqlInsightsKeyPathSummary(KeyPath, IdSession, Data) Values(@KeyPath, @IdSession, @Data);",
                sqlUpdate = "Update SqlInsightsKeyPathSummary Set Data = @Data Where KeyPath = @KeyPath And IdSession = @IdSession";

            var aliveSessions = GetAliveSessions();
            foreach (var idSession in aliveSessions)
            {
                using (IDbConnection con = GetConnection())
                {
                    var keyPath = SerializeKeyPath(reqAction.Key);

                    // SUMMARY: SqlInsightsKeyPathSummary
                    ActionSummaryCounters actionActionSummary = reqAction.AsSummary();
                    string rawDataPrev = con
                        .Query<SelectDataResult>(sqlSelect, new {IdSession = idSession, KeyPath = keyPath})
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
                    con.Execute(sqlUpsert, new {KeyPath = keyPath, IdSession = idSession, Data = dataSummary});

                    // DETAILS: SqlInsightsAction
                    const string sqlDetail = "Insert SqlInsightsAction(At, IdSession, KeyPath, IsOK, Data) Values(@At, @IdSession, @KeyPath, @IsOK, @Data)";
                    var detail = reqAction;
                    var dataDetail = JsonConvert.SerializeObject(detail, DefaultSettings);
                    con.Execute(sqlDetail, new
                    {
                        At = detail.At,
                        IsOK = string.IsNullOrEmpty(detail.BriefException),
                        IdSession = idSession,
                        KeyPath = keyPath,
                        Data = dataDetail,
                    });
                }
            }
        }
        
        public IEnumerable<long> GetAliveSessions()
        {
            const string sql = "Select IdSession From SqlInsightsSession Where IsFinished = (0)";
            using (var con = GetConnection())
            {
                var query = con.Query<long>(sql, null);
                return query.ToList();
            }
        }
        
        static string SerializeKeyPath(SqlInsightsActionKeyPath keyPath)
        {
            return keyPath == null || keyPath.Path == null ? null : string.Join("\x2192", keyPath.Path);
        }

        static SqlInsightsActionKeyPath ParseKeyPath(string keyPath)
        {
            return keyPath == null ? null : new SqlInsightsActionKeyPath(keyPath.Split((char) 0x2192));
        }



#if NETSTANDARD
        public async Task<IEnumerable<ActionSummaryCounters>> GetActionsSummary(long idSession)
        {
            const string sql = "Select KeyPath, Data From SqlInsightsKeyPathSummary Where IdSession = @IdSession";
            using (var con = GetConnection())
            {
                IEnumerable<SelectDataResult> resultSet = await con.QueryAsync<SelectDataResult>(sql, new {IdSession = idSession});
                var query = resultSet.Select(x =>
                {
                    return JsonConvert.DeserializeObject<ActionSummaryCounters>(x.Data, DefaultSettings);
                });

                return query.ToList();
            }
        }

        public async Task<string> GetActionsSummaryTimestamp(long idSession)
        {
            const string sql = "Select Top 1 Version From SqlInsightsKeyPathSummary Where IdSession = @IdSession Order By Version Desc";
            using (var con = GetConnection())
            {
                var query = await con.QueryAsync<SelectVersionResult>(sql, new { IdSession = idSession});
                byte[] binaryVersion = query.FirstOrDefault()?.Version;
                ulong? version = RowVersion2Int64(binaryVersion);
                return version == null ? "" : version.Value.ToString();
            }
        }

        static UInt64? RowVersion2Int64(byte[] binaryVersion)
        {
            if (binaryVersion != null && binaryVersion.Length == 8)
            {
                UInt64 ret = 0;
                UInt64 mult = 1;
                for (int i = 7; i >= 0; i--)
                {
                    ret += mult * binaryVersion[i];
                    mult = mult << 8;
                }

                return ret;
            }
            else if (binaryVersion != null && binaryVersion.Length != 8)
                throw new InvalidOperationException("Sql version field should have 8 bytes length");

            return null;
        }

        class SelectIdActionResult
        {
            public long IdAction { get; set; }
        }
        class SelectVersionResult
        {
            public byte[] Version { get; set; }
        }

        public async Task<string> GetKeyPathTimestampOfDetails(long idSession, SqlInsightsActionKeyPath keyPath)
        {
            const string sql = "Select Top 1 IdAction From SqlInsightsAction Where KeyPath = @KeyPath And IdSession = @IdSession Order By At Desc";
            using (var con = GetConnection())
            {
                var query = await con.QueryAsync<SelectIdActionResult>(sql, new {KeyPath = SerializeKeyPath(keyPath), IdSession = idSession});
                long? ret = query.FirstOrDefault()?.IdAction;
                return ret.HasValue ? ret.Value.ToString() : "";
            }
        }

        public async Task<IEnumerable<ActionDetailsWithCounters>> GetActionsByKeyPath(long idSession, SqlInsightsActionKeyPath keyPath)
        {
            const string sql = "Select Top 100 Data From SqlInsightsAction Where KeyPath = @KeyPath And IdSession = @IdSession Order By At Desc";
            using (var con = GetConnection())
            {
                var query = await con
                    .QueryAsync<SelectDataResult>(sql, new {KeyPath = SerializeKeyPath(keyPath), IdSession = idSession});

                var ret = query
                    .Select(x => JsonConvert.DeserializeObject<ActionDetailsWithCounters>(x.Data, DefaultSettings));

                return ret.ToList();
            }
        }

        public async Task<IEnumerable<SqlInsightsSession>> GetSessions()
        {
            const string sql = "Select IdSession, StartedAt, EndedAt, IsFinished, Caption, MaxDurationMinutes From SqlInsightsSession";
            using (var con = GetConnection())
            {
                var query = await con.QueryAsync<SqlInsightsSession>(sql, null);
                return query.ToList();
            }
        }


        public async Task<long> CreateSession(string caption, int? maxDurationMinutes)
        {
            const string sql = @"
Insert SqlInsightsSession(StartedAt, IsFinished, Caption, MaxDurationMinutes) Values(
    GetUtcDate(),
    (0),
    @Caption,
    @MaxDurationMinutes
);   
Select SCOPE_IDENTITY();  
";

            using (var con = GetConnection())
            {
                var ret = await con.ExecuteScalarAsync<long>(sql, new {Caption = caption, MaxDurationMinutes = maxDurationMinutes});
                return ret;
            }
            
        }

        public async Task DeleteSession(long idSession)
        {
            const string sql = @"
Delete From SqlInsightsAction Where IdSession = @IdSession;
Delete From SqlInsightsKeyPathSummary Where IdSession = @IdSession;
Delete From SqlInsightsSession Where IdSession = @IdSession;
";
        
            using (var con = GetConnection())
            {
                await con.ExecuteAsync(sql, new {IdSession = idSession});
            }
        }

        public async Task RenameSession(long idSession, string caption)
        {
            const string sql = @"Update SqlInsightsSession Set Caption = @Caption Where IdSession = @IdSession;"; 
            using (var con = GetConnection())
            {
                await con.ExecuteAsync(sql, new {IdSession = idSession});
            }
        }

        public async Task FinishSession(long idSession)
        {
            if (idSession == 0) return;
            const string sql = @"Update SqlInsightsSession Set IsFinished = (1), EndedAt = GETUTCDATE() Where IdSession = @IdSession;"; 
            using (var con = GetConnection())
            {
                await con.ExecuteAsync(sql, new {IdSession = idSession});
            }
        }
        
#endif
        
    }
}