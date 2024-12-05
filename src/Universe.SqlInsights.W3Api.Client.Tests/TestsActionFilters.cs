using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Slices.Dashboard;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
    [TestFixture]
    public class TestsActionFilters
    {
        [Test]
        [TestCase("First")]
        [TestCase("Next")]
        public async Task ShowFilters(string kind)
        {
            S5ApiClient client = S5ApiClientFactory.Create();
            var filters = await client.PopulateFiltersAsync();
            Console.WriteLine($"filters.ApplicationList (count = {filters?.ApplicationList?.Count})");
            foreach (var appFilter in filters?.ApplicationList)
                Console.WriteLine(" * " + appFilter.App);
            Console.WriteLine($"filters.HostIdList (count = {filters?.HostIdList?.Count})");
            foreach (var hostId in filters?.HostIdList)
                Console.WriteLine(" * " + hostId.HostId);
        }
    }
}