﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using DbProviderFactory = System.Data.Common.DbProviderFactory;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class SeedTestCaseProvider : TestCaseProvider
    {
        public int ThreadCount { get; set; }
        public int LimitCount { get; set; }
        
        public override string ToString()
        {
            return $"{ThreadCount}T up to {LimitCount:n0} by {Provider.GetShortProviderName()}, {(NeedMot == true ? "MOT On" : "MOT Off")}";
        }
        
        public new static IEnumerable<SeedTestCaseProvider> GetTestCases()
        {
            var threadsList = new[] { 1, Environment.ProcessorCount + 1, Environment.ProcessorCount * 2 + 1 };
            if (TestEnv.IsTestUnderCoverage) threadsList = new[] { Environment.ProcessorCount };
            var providerList = new DbProviderFactory[] { Microsoft.Data.SqlClient.SqlClientFactory.Instance, System.Data.SqlClient.SqlClientFactory.Instance, };
            bool isMotSupported = TestEnv.IsMotSupported();
            bool?[] motList = isMotSupported ? new bool?[] { true, false } : new bool?[] { null };

            // JIT
            foreach (var mot in motList)
            foreach (var provider in providerList)
            {
                var dbNameParameters = $"{provider.GetShortProviderName()} {(mot == true ? "MOT On" : "MOT Off")}";
                SqlConnectionStringBuilder connectionString = new SqlConnectionStringBuilder(TestEnv.TheConnectionString);
                connectionString.InitialCatalog = string.Format(TestEnv.DbNamePattern, dbNameParameters, Environment.MachineName);
                yield return new SeedTestCaseProvider()
                {
                    Provider = provider,
                    ConnectionString = connectionString.ConnectionString,
                    LimitCount = 100,
                    ThreadCount = threadsList.Max(),
                    NeedMot = mot,
                };
            }

            foreach (var threads in threadsList)
            foreach (var provider in providerList)
            foreach (var mot in motList)
            {
                var dbNameParameters = $"{provider.GetShortProviderName()} {(mot == true ? "MOT On" : "MOT Off")}";
                SqlConnectionStringBuilder connectionString = new SqlConnectionStringBuilder(TestEnv.TheConnectionString);
                connectionString.InitialCatalog = string.Format(TestEnv.DbNamePattern, dbNameParameters, Environment.MachineName);

                yield return new SeedTestCaseProvider()
                {
                    Provider = provider,
                    ConnectionString = connectionString.ConnectionString,
                    LimitCount = TestEnv.IsTestUnderCoverage ? 500 : 5000,
                    ThreadCount = threads,
                    NeedMot = mot,
                };
            }
        }
        

    }
}