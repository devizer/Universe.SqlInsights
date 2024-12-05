using System.Collections.Generic;
using System.Linq;
using Slices.Dashboard;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
    public class ActionDetailsTestCase
    {
        public SqlInsightsSession Session { get; set; }
        public long SessionId => Session?.IdSession ?? -42;
        public string[] KeyPath { get; set; }

        public override string ToString()
        {
            var ret = string.Join(" → ", KeyPath) + $" for session #{SessionId} '{Session.Caption}'";
            return ret;
        }

        public static IEnumerable<ActionDetailsTestCase> GetTestCases()
        {
            S5ApiClient client = S5ApiClientFactory.Create();
            ICollection<SqlInsightsSession> sessions = client.SessionsAsync().GetSafeResult();
            
            sessions = sessions.Concat(new[] { new SqlInsightsSession() {IdSession = -42, Caption = "Fake Session"} }).ToList();

            foreach (var session in sessions)
            {
                ActionsSummaryParameters request = new ActionsSummaryParameters()
                {
                    AppsFilter = null,
                    HostsFilter = null,
                    IdSession = session.IdSession,
                };

                ICollection<ActionSummaryCounters> actionsWithSummary = client.SummaryAsync(request).GetSafeResult();
                foreach (var action in actionsWithSummary)
                {
                    yield return new ActionDetailsTestCase()
                    {
                        Session = session,
                        KeyPath = action.Key.Path.ToArray()
                    };
                }

            }
        }
    }
}