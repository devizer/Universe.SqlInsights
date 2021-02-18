using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage
{
    public class SqlServerSqlInsightsStorage : ISqlInsightsStorage
    {
        public readonly DbProviderFactory ProviderFactory;
        public readonly string ConnectionString;

        private static volatile bool AreMigrationsChecked = false;
        private static readonly object SyncMigrations = new object();

        public SqlServerSqlInsightsStorage(DbProviderFactory providerFactory, string connectionString)
        {
            ProviderFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
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
            // Migrations here for development only
            // TODO: Multiple Storages per app?
            if (!AreMigrationsChecked)
                lock(SyncMigrations)
                    if (!AreMigrationsChecked)
                    {
                        new SqlServerSqlInsightsMigrations(ProviderFactory, ConnectionString).Migrate();
                        AreMigrationsChecked = true;
                    }

            var ret = ProviderFactory.CreateConnection();
            ret.ConnectionString = ConnectionString;
            ret.Open();
            return ret;
        }

        class SelectDataResult
        {
            public string Data { get; set; }
        }

        public void AddAction(ActionDetailsWithCounters reqAction)
        {

            if (reqAction.AppName == null) throw new ArgumentException("Missing reqAction.AppName");

            const string
                sqlSelect = "Select Data From SqlInsightsKeyPathSummary WITH (UPDLOCK) Where KeyPath = @KeyPath And HostId = @HostId And AppName = @AppName And IdSession = @IdSession",
                sqlInsert = "Insert SqlInsightsKeyPathSummary(KeyPath, IdSession, AppName, HostId, Data) Values(@KeyPath, @IdSession, @AppName, @HostId, @Data);",
                sqlUpdate = "Update SqlInsightsKeyPathSummary Set Data = @Data Where KeyPath = @KeyPath And HostId = @HostId And AppName = @AppName And IdSession = @IdSession";

            var aliveSessions = GetAliveSessions().ToList();
            if (aliveSessions.Count <= 0) return;
            
            using (IDbConnection con = GetConnection())
            {
                var tran = con.BeginTransaction(IsolationLevel.ReadCommitted);
                using (tran)
                {
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
                        con.Execute(sqlUpsert, new
                        {
                            KeyPath = keyPath,
                            IdSession = idSession,
                            Data = dataSummary,
                            AppName = idAppName,
                            HostId = idHostId,
                        }, tran);

                        // DETAILS: SqlInsightsAction
                        const string sqlDetail =
                            "Insert SqlInsightsAction(At, IdSession, KeyPath, IsOK, AppName, HostId, Data) Values(@At, @IdSession, @KeyPath, @IsOK, @AppName, @HostId, @Data)";
                        var detail = reqAction;
                        var dataDetail = JsonConvert.SerializeObject(detail, DefaultSettings);
                        con.Execute(sqlDetail, new
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
                    
                    tran.Commit();
                }
            }
        }
        
        public IEnumerable<long> GetAliveSessions()
        {
            const string sql = @"Select IdSession From SqlInsightsSession Where IsFinished = (0) And (MaxDurationMinutes Is Null Or DateAdd(minute,MaxDurationMinutes,StartedAt) >= GetUtcDate())";
            using (var con = GetConnection())
            {
                var query = con.Query<long>(sql, null);
                return query.ToList();
            }
        }
        
        static string SerializeKeyPath(SqlInsightsActionKeyPath keyPath)
        {
            return keyPath?.Path == null ? null : string.Join("\x2192", keyPath.Path);
        }

        static SqlInsightsActionKeyPath ParseKeyPath(string keyPath)
        {
            return keyPath == null ? null : new SqlInsightsActionKeyPath(keyPath.Split((char) 0x2192));
        }


#if NETSTANDARD
        public async Task<IEnumerable<ActionSummaryCounters>> GetActionsSummary(long idSession, string optionalApp = null, string optionalHost = null)
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

        public async Task<string> GetActionsSummaryTimestamp(long idSession, string optionalApp = null, string optionalHost = null)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            if (lastN < 1) throw new ArgumentOutOfRangeException(nameof(lastN));
            const string sql = "Select Top (@N) Data From SqlInsightsAction Where KeyPath = @KeyPath And IdSession = @IdSession Order By IdAction Desc";
            using (var con = GetConnection())
            {
                var query = await con
                    .QueryAsync<SelectDataResult>(sql, new {KeyPath = SerializeKeyPath(keyPath), IdSession = idSession, N = lastN});

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
                await con.ExecuteAsync(sql, new {IdSession = idSession, Caption = caption});
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

        public async Task<IEnumerable<LongAndString>> GetAppNames()
        {
            StringsStorage strings = new StringsStorage(GetConnection(), null);
            return await strings.GetAllStringsByKind(StringKind.AppName);
        }

        public async Task<IEnumerable<LongAndString>> GetHostIds()
        {
            StringsStorage strings = new StringsStorage(GetConnection(), null);
            return await strings.GetAllStringsByKind(StringKind.HostId);
        }

#endif
        
    }
}