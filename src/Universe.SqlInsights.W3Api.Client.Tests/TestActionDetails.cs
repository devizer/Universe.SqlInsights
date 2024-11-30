using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Slices.Dashboard;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
    [TestFixture]
    public class TestActionDetails
    {
        [Test]
        [TestCaseSource(typeof(SessionsTestCase), nameof(SessionsTestCase.GetTestCases))]
        public async Task TestSimple(SessionsTestCase testCase)
        {
            S5ApiClient client = S5ApiClientFactory.Create();

            ActionsSummaryParameters request = new ActionsSummaryParameters()
            {
                AppsFilter = null,
                HostsFilter = null,
                IdSession = testCase.SessionId,
            };

            var actionsWithSummary = await client.SummaryAsync(request);
            var firstPath = actionsWithSummary.FirstOrDefault()?.Key ?? new SqlInsightsActionKeyPath() { Path = new[] { "None" } };

            ActionsParameters actionParameters = new ActionsParameters()
            {
                AppsFilter = null,
                HostsFilter = null,
                IdSession = testCase.SessionId,
                Path = firstPath.Path
            };
            var details = await client.ActionsByKeyAsync(actionParameters);

            Console.WriteLine($"Details for [{string.Join("→", firstPath.Path)}] (count={details.Count}){Environment.NewLine}{details.AsJson()}");
        }


        [Test]
        [TestCaseSource(typeof(ActionDetailsTestCase), nameof(ActionDetailsTestCase.GetTestCases))]
        public async Task TestFullActions(ActionDetailsTestCase testCase)
        {
            S5ApiClient client = S5ApiClientFactory.Create();


            ActionsParameters actionParameters = new ActionsParameters()
            {
                AppsFilter = null,
                HostsFilter = null,
                IdSession = testCase.SessionId,
                Path = testCase.KeyPath
            };
            var details = await client.ActionsByKeyAsync(actionParameters);

            Console.WriteLine($"Details for [{string.Join("→", testCase.KeyPath)}] (count={details.Count}){Environment.NewLine}{details.AsJson()}");
        }
    }
}
