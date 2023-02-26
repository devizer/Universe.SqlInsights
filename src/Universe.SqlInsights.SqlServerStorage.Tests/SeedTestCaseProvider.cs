using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class SeedTestCaseProvider
    {
        public DbProviderFactory Provider { get; set; }
        public string ConnectionString { get; set; }
        public int ThreadCount { get; set; }
        public int LimitCount { get; set; }
        
        public override string ToString()
        {
            return $"{ThreadCount}T up to {LimitCount:n0} by " + Path.GetFileNameWithoutExtension(Provider.GetType().Assembly.Location);
        }
        
        public static IEnumerable<SeedTestCaseProvider> GetList()
        {
            var threadsList = new[] { Environment.ProcessorCount*2 + 1, Environment.ProcessorCount + 1, 1};
            var cases = TestCaseProvider.GetList();
            foreach (var c in cases)
            {
                yield return new SeedTestCaseProvider()
                {
                    Provider = c.Provider,
                    ConnectionString = c.ConnectionString,
                    LimitCount = 100,
                    ThreadCount = threadsList.Max(),
                };
            }

            foreach (int threads in threadsList)
            foreach (var c in cases)
            {
                yield return new SeedTestCaseProvider()
                {
                    Provider = c.Provider,
                    ConnectionString = c.ConnectionString,
                    LimitCount = 5000,
                    ThreadCount = threads
                };
            }
        }
        
    }
}