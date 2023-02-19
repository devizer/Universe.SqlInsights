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
    public partial class SqlServerSqlInsightsStorage : ISqlInsightsStorage
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

        class SelectKeyAndDataResult
        {
            public string KeyPath { get; set; }
            public string Data { get; set; }
        }
        class SelectDataResult
        {
            public string Data { get; set; }
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
            StringBuilder sql = new StringBuilder("Select KeyPath, Data From SqlInsightsKeyPathSummary Where IdSession = @IdSession");
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

                var sql = $"Select Max(Version) From SqlInsightsKeyPathSummary Where IdSession = @IdSession {sqlWhere}";
                var query = await con.QueryAsync<long?>(sql, sqlParams);
                long? binaryVersion = query.FirstOrDefault();
                return binaryVersion.ToString();
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

        public async Task<IEnumerable<LongAndString>> GetAppNames()
        {
            return await GetAllStringsByKind(StringKind.AppName);
        }

        public async Task<IEnumerable<LongAndString>> GetHostIds()
        {
            return await GetAllStringsByKind(StringKind.HostId);
        }

        private async Task<IEnumerable<LongAndString>> GetAllStringsByKind(StringKind stringKind)
        {
            using var dbConnection = GetConnection();
            StringsStorage strings = new StringsStorage(dbConnection, null);
            return await strings.GetAllStringsByKind(stringKind);
        }

#endif
        
    }
}