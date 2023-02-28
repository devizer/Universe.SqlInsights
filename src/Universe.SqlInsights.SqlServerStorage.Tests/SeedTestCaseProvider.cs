﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using DbProviderFactory = System.Data.Common.DbProviderFactory;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class SeedTestCaseProvider : TestCaseProvider
    {
        public int ThreadCount { get; set; }
        public int LimitCount { get; set; }
     
        public bool? NeedMot { get; set; }
        
        public override string ToString()
        {
            return $"{ThreadCount}T up to {LimitCount:n0} by {Provider.GetShortProviderName()}, {(NeedMot == true ? "MOT On" : "MOT Off")}";
        }
        
        public static new IEnumerable<SeedTestCaseProvider> GetTestCases()
        {
            var threadsList = new[] { Environment.ProcessorCount * 2 + 1, Environment.ProcessorCount + 1, 1 };
            var limitsList = new[] { 100, 5000 };
            var providerList = new DbProviderFactory[] { System.Data.SqlClient.SqlClientFactory.Instance, Microsoft.Data.SqlClient.SqlClientFactory.Instance };
            bool isMotSupported = TestEnv.IsMotSupported();
            bool?[] motList = isMotSupported ? new bool?[] { true, false } : new bool?[] { null };
            foreach (var provider in providerList)
            foreach (var limits in limitsList)
            foreach (var threads in threadsList)
            foreach (var mot in motList)
            {
                var dbNameParameters = $"{provider.GetShortProviderName()} {(mot == true ? "MOT On" : "MOT Off")}";
                SqlConnectionStringBuilder connectionString = new SqlConnectionStringBuilder(TestEnv.TheConnectionString);
                connectionString.InitialCatalog = string.Format(TestEnv.DbNamePattern, dbNameParameters);

                yield return new SeedTestCaseProvider()
                {
                    Provider = provider,
                    ConnectionString = connectionString.ConnectionString,
                    LimitCount = limits,
                    ThreadCount = threads,
                    NeedMot = mot,
                };
            }
        }
        

    }
}