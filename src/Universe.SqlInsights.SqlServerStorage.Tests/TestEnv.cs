using System;
using System.IO;
using System.Text;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class TestEnv
    {
        // private string ConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Database=SqlServerSqlInsightsStorage_Tests; Integrated Security=SSPI";
        // private static readonly string TheConnectionString = "Data Source=(local);Database=SqlServerSqlInsightsStorage_Tests; Integrated Security=SSPI";
        public static string TheConnectionString => string.IsNullOrEmpty(DbConnectionString) ? "Data Source=(local);Integrated Security=SSPI" : DbConnectionString;
        public static readonly string DbNamePattern = "SqlInsights Storage {0} Tests";

        // OPTIONAL
        public static string DbDataDir => Environment.GetEnvironmentVariable("SQLINSIGHTS_DATA_DIR");
        public static string DbConnectionString => Environment.GetEnvironmentVariable("SQLINSIGHTS_CONNECTION_STRING");
        
        public static string TestConfigName => Environment.GetEnvironmentVariable("TEST_CONFIGURATION");
        public static string TestCpuName => Environment.GetEnvironmentVariable("TEST_CPU_NAME");

        public static void LogToArtifact(string fileName, string line)
        {
            var artifactDirectory = Environment.GetEnvironmentVariable("SYSTEM_ARTIFACTSDIRECTORY");
            if (!string.IsNullOrEmpty(artifactDirectory))
            {
                var fullName = Path.Combine(artifactDirectory, fileName);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullName));
                }
                catch
                {
                }

                using(FileStream fs = new FileStream(fullName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using(StreamWriter wr = new StreamWriter(fs, new UTF8Encoding(false)))
                        wr.WriteLine(line);
                
            }
        }
    }
}