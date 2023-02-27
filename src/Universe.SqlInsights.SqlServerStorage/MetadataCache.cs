using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Universe.SqlServerJam;

namespace Universe.SqlInsights.SqlServerStorage
{
    // Workaround for Memory Optimized Data Access
    public static class MetadataCache
    {
        class Metadata
        {
            public bool IsMemoryOptimized;
        }

        private static object _sync = new object();

        public static bool IsMemoryOptimized(IDbConnection connection)
        {
            return GetMetadata(connection).IsMemoryOptimized;
        }

        public static bool IsMemoryOptimized(string connectionString)
        {
            SqlConnection cnn = new SqlConnection(connectionString);
            return GetMetadata(cnn).IsMemoryOptimized;
        }

        static Metadata GetMetadata(IDbConnection connection)
        {
                if (_Cache == null)
                    lock(_sync)
                        if (_Cache == null)
                        {
                            var ret = GetMetadataImplementation(connection);
                            _Cache = ret;
                            return ret;
                        }
                
                return _Cache;
        }

        private static volatile Metadata _Cache;

        private static Metadata GetMetadataImplementation(IDbConnection connection)
        {
            bool isSupported = connection.Manage().IsMemoryOptimizedTableSupported;
            if (isSupported)
            {
                var motFileGroup = connection.Query<string>("Select Top 1 name from sys.filegroups where type = 'FX'").FirstOrDefault();
                isSupported = !string.IsNullOrEmpty(motFileGroup);
            }

            return new Metadata()
            {
                IsMemoryOptimized = isSupported
            };
        }

        public static void ResetCacheForTests()
        {
            _Cache = null;
        }
    }
}
