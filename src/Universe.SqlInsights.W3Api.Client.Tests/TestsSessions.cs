using NUnit.Framework;
using Slices.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlInsights.W3Api.Client.GeneratedClient;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
    [TestFixture]
    public class TestsSessions
    {
        [Test]
        [TestCase("First")]
        [TestCase("Next")]
        public async Task TestGetList(string kind)
        {
            S5ApiClient client = S5ApiClientFactory.Create();
            ICollection<SqlInsightsSession> sessions = await client.SessionsAsync();
            sessions.PopulateSessionsCalculatedFields();
            Console.WriteLine(sessions.AsJson());
        }
    }
}
