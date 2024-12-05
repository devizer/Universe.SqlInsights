using System;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Slices.Dashboard;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
    [TestFixture]
    public class TestsAbout
    {
        [Test]
        [TestCase("First")]
        [TestCase("Next")]
        public async Task TestSimple(string kind)
        {
            S5ApiClient client = new S5ApiClient(TestEnvironment.W3ApiBaseUri, new HttpClient());
            var about = await client.IndexAsync();
            Console.WriteLine(about.AsJson());
        }

        private static HttpClient _HttpClient = new HttpClient();
        [Test]
        [TestCase("First")]
        [TestCase("Next")]
        public async Task TestReused(string kind)
        {
            S5ApiClient client = new S5ApiClient(TestEnvironment.W3ApiBaseUri, _HttpClient);
            var about = await client.IndexAsync();
            Console.WriteLine(about.AsJson());
        }
    }
}