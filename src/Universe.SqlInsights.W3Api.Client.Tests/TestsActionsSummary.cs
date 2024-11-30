using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Slices.Dashboard;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
    [TestFixture]
    public class TestsActionsSummary
    {
        [Test]
        [TestCaseSource(typeof(SessionsTestCase), nameof(SessionsTestCase.GetTestCases))]
        public async Task TestGetActionsSummaryList(SessionsTestCase testCase)
        {
            S5ApiClient client = S5ApiClientFactory.Create();

            ActionsSummaryParameters request = new ActionsSummaryParameters()
            {
                AppsFilter = null,
                HostsFilter = null,
                IdSession = testCase.SessionId,
            };

            var actionsWithSummary = await client.SummaryAsync(request);
            Console.WriteLine($"Actions With SUMMARY (count={actionsWithSummary.Count}){Environment.NewLine}{actionsWithSummary.AsJson()}");
        }
    }
}