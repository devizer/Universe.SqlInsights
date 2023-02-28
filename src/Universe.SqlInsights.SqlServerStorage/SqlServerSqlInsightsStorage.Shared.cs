using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
            Counter = Interlocked.Increment(ref CounterStorage);
        }

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

        
        static string SerializeKeyPath(SqlInsightsActionKeyPath keyPath)
        {
            return keyPath?.Path == null ? null : string.Join("\x2192", keyPath.Path);
        }

        static SqlInsightsActionKeyPath ParseKeyPath(string keyPath)
        {
            return keyPath == null ? null : new SqlInsightsActionKeyPath(keyPath.Split((char) 0x2192));
        }


#if NETSTANDARD

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

        class SelectVersionResult
        {
            public byte[] Version { get; set; }
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