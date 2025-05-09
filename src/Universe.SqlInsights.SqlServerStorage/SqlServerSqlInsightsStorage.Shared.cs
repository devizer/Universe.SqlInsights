﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage
{
    public partial class SqlServerSqlInsightsStorage : ISqlInsightsStorage, ITraceableStorage
    {
        public readonly DbProviderFactory ProviderFactory;
        public string ConnectionString { get; set; }

        private static volatile bool AreMigrationsChecked = false;
        private static readonly object SyncMigrations = new object();

        public ICrossPlatformLogger Logger { get; set; } = NullCrossPlatformLogger.Instance;

        public SqlServerSqlInsightsStorage(DbProviderFactory providerFactory, string connectionString)
        {
            ProviderFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Counter = Interlocked.Increment(ref CounterStorage);
        }
        
        public static void ResetCacheForTests()
        {
            // TODO: DI for net framework
            AreMigrationsChecked = false;
            StringsStorage.ResetCacheForTests();
            MetadataCache.ResetCacheForTests();
        }


        IDbConnection GetConnection()
        {
            // FlushDapperMemoryLeaks();

            // Migrations here for development only
            // TODO: Multiple Storages per app?
            if (!AreMigrationsChecked)
                lock(SyncMigrations)
                    if (!AreMigrationsChecked)
                    {
                        var migrations = new SqlServerSqlInsightsMigrations(ProviderFactory, ConnectionString);
                        migrations.Migrate();
                        Logger?.LogInformation($"IMPLICIT MIGRATIONs LOGS{Environment.NewLine}{migrations.Logs}");
                        AreMigrationsChecked = true;
                    }

            var ret = ProviderFactory.CreateConnection();
            ret.ConnectionString = ConnectionString;
            // Console.WriteLine($"[DEBUG ConnectionString] GetConnection() '{ret.ConnectionString}'");
            /*
            ret.StateChange += (sender, args) =>
            {
                Console.WriteLine($"[DEBUG ConnectionString] State Changed to {args.CurrentState}: '{ret.ConnectionString}'");
            };
            */
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


#if NETSTANDARD || NET5_0 || NET461 || NET5_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        static UInt64? RowVersion2Int64(byte[] binaryVersion)
        {
            if (binaryVersion != null && binaryVersion.Length == 8)
            {
                UInt64 ret = 0;
                UInt64 mult = 1;
                for (int i = 7; i >= 0; i--)
                {
                    ret += mult * binaryVersion[i];
                    mult <<= 8;
                }

                return ret;
            }
            else if (binaryVersion != null && binaryVersion.Length != 8)
                throw new InvalidOperationException("Sql version field should have 8 bytes length");

            return null;
        }

        // W3API Only
#if NETSTANDARD || NET5_0 || NET461 || NET5_0_OR_GREATER
        class SelectVersionResult
        {
            public byte[] Version { get; set; }
        }


        public async Task<IEnumerable<LongIdAndString>> GetAppNames()
        {
            return await GetAllStringsByKind(StringKind.AppName);
        }

        public async Task<IEnumerable<LongIdAndString>> GetHostIds()
        {
            return await GetAllStringsByKind(StringKind.HostId);
        }

        private async Task<IEnumerable<LongIdAndString>> GetAllStringsByKind(StringKind stringKind)
        {
            using var dbConnection = GetConnection();
            StringsStorage strings = new StringsStorage(dbConnection, null);
            return await strings.GetAllStringsByKind(stringKind);
        }

#endif
        
    }
}