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

        [Test]
        [TestCase("First")]
        [TestCase("Next")]
        public async Task TestCRUD(string kind)
        {
            Console.WriteLine($"DateTimeOffset.UtcNow = '{DateTimeOffset.UtcNow.AsJson()}'");
            Console.WriteLine($"DateTimeOffset.Now = '{DateTimeOffset.Now.AsJson()}'");
            Console.WriteLine($"TimeZoneInfo.Local = {TimeZoneInfo.Local}");
            Console.WriteLine($"TimeZoneInfo.Utc = {TimeZoneInfo.Utc}");

            var d3 = DateTimeOffset.Now.Subtract(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
            Console.WriteLine($"d3 = {d3}");

            DateTime d1 = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local);
            DateTimeOffset d2 = DateTimeOffset.Now;
            Console.WriteLine($"d1 = {d1}");
            Console.WriteLine($"d2 = {d2}");

            S5ApiClient client = S5ApiClientFactory.Create();
            long? sessionid = null;
            try
            {
                // Create
                CreateSessionParameters createSessionParameters = new CreateSessionParameters()
                {
                    Caption = $"Test Session {kind} {Guid.NewGuid()}",
                    MaxDurationMinutes = 13,
                };
                sessionid = await client.CreateSessionAsync(createSessionParameters);
                Console.WriteLine($"Session Created: {sessionid}");

                // Fetch
                ICollection<SqlInsightsSession> sessions = await client.SessionsAsync();
                sessions.PopulateSessionsCalculatedFields();
                Console.WriteLine(sessions.AsJson());

            }
            finally
            {
                // Delete
                if (sessionid.HasValue)
                {
                    await client.DeleteSessionAsync(new IdSessionParameters() { IdSession = sessionid.Value });
                    Console.WriteLine($"Session {sessionid} successfully deleted");
                }
            }
        }
    }
}
