﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace ConnectionStringBuilderBenchmark
{
    public class Program
    {
        public static void Main()
        {
            var summary = BenchmarkRunner.Run(typeof(ConnectionStringBenchmark).Assembly);
        }
    }

    public class ConnectionStringBenchmark
    {
        private static readonly string TheConnectionString = "Data Source=(local);Database=SqlServerSqlInsightsStorage_Tests; Integrated Security=SSPI";

        [Benchmark] // 2 microseconds
        public int SystemBenchmark()
        {
            System.Data.SqlClient.SqlConnectionStringBuilder b = new System.Data.SqlClient.SqlConnectionStringBuilder(TheConnectionString);
            var server = b.DataSource;
            var db = b.InitialCatalog;
            return server.Length + db.Length;
        }

        [Benchmark] // 2 microseconds
        public int MicrosoftBenchmark()
        {
            Microsoft.Data.SqlClient.SqlConnectionStringBuilder b = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(TheConnectionString);
            var server = b.DataSource;
            var db = b.InitialCatalog;
            return server.Length + db.Length;
        }
    }
}


