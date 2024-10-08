﻿using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using Universe.SqlServerJam;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public static class TestEnv
    {
        // private string ConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Database=SqlServerSqlInsightsStorage_Tests; Integrated Security=SSPI";
        // private static readonly string TheConnectionString = "Data Source=(local);Database=SqlServerSqlInsightsStorage_Tests; Integrated Security=SSPI";
        public static string TheConnectionString => string.IsNullOrEmpty(OptionalDbConnectionString) ? "Data Source=(local);Integrated Security=SSPI;TrustServerCertificate=True" : OptionalDbConnectionString;
        
        public static readonly string DbNamePattern = "SqlInsights {0} Tests for {1}";

        // OPTIONAL
        public static string OptionalDbDataDir => Environment.GetEnvironmentVariable("SQLINSIGHTS_DATA_DIR");
        public static string OptionalDbConnectionString => Environment.GetEnvironmentVariable("SQLINSIGHTS_CONNECTION_STRING");
        
        public static string TestConfigName => Environment.GetEnvironmentVariable("TEST_CONFIGURATION");
        public static string TestCpuName => Environment.GetEnvironmentVariable("TEST_CPU_NAME");
        public static bool IsTestUnderCoverage => GetBooleanVar("TEST_UNDER_COVERAGE");

        static bool GetBooleanVar(string name)
        {
            var raw = Environment.GetEnvironmentVariable(name);
            return raw == "1" || "True".Equals(raw, StringComparison.OrdinalIgnoreCase) || "On".Equals(raw, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetShortProviderName(this DbProviderFactory provider)
        {
            var providerName = Path.GetFileNameWithoutExtension(provider.GetType().Assembly.Location);
            providerName = providerName.Split('.').First();
            return providerName;
        }
        
        public static bool IsMotSupported()
        {
            SqlConnectionStringBuilder master = new SqlConnectionStringBuilder(TheConnectionString);
            master.InitialCatalog = "";
            SqlConnection con = new SqlConnection(master.ConnectionString);
            return con.Manage().IsMemoryOptimizedTableSupported;
        }

        
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