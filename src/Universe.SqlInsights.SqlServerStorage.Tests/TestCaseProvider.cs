using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class TestCaseProvider
    {
        public DbProviderFactory Provider { get; set; }
        public string ConnectionString { get; set; }
        public bool? NeedMot { get; set; }

        public override string ToString()
        {
            return $"{Provider.GetShortProviderName()}, {(NeedMot == true ? "MOT On" : "MOT Off")}";
        }
        
        public static IEnumerable<TestCaseProvider> GetTestCases()
        {
            var providerList = new DbProviderFactory[] { System.Data.SqlClient.SqlClientFactory.Instance, Microsoft.Data.SqlClient.SqlClientFactory.Instance };
            bool isMotSupported = TestEnv.IsMotSupported();
            bool?[] motList = isMotSupported ? new bool?[] { true, false } : new bool?[] { null };
            foreach (var provider in providerList)
            foreach (var mot in motList)
            {
                var dbNameParameters = $"{provider.GetShortProviderName()} {(mot == true ? "MOT On" : "MOT Off")}";
                SqlConnectionStringBuilder connectionString = new SqlConnectionStringBuilder(TestEnv.TheConnectionString);
                var serverVersion = TestEnv.GetTheServerVersion();
                connectionString.InitialCatalog = string.Format(TestEnv.DbNamePattern, dbNameParameters, Environment.MachineName)
                    + (serverVersion == null ? "" : $" v{serverVersion}");

                yield return new TestCaseProvider()
                {
                    Provider = provider,
                    ConnectionString = connectionString.ConnectionString,
                    NeedMot = mot,
                };
            }
        }


    }
}