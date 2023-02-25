using System.Collections.Generic;
using System.Data.Common;
using System.IO;

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
            var cases = TestCaseProvider.GetList();
            foreach (int threads in new[] { 8, 1})
            foreach (int count in new[] { threads * 3, 5000 })
            foreach (var c in cases)
            {
                yield return new SeedTestCaseProvider()
                {
                    Provider = c.Provider,
                    ConnectionString = c.ConnectionString,
                    LimitCount = count,
                    ThreadCount = threads
                };
            }
        }


        
    }
}