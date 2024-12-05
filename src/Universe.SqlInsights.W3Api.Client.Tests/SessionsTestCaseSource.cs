using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slices.Dashboard;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
    public class SessionsTestCase
    {
        public SqlInsightsSession Session { get; set; }
        public long SessionId => Session?.IdSession ?? -42;

        public override string ToString()
        {
            return Session == null ? "Not Existing Session" : $"Session #{SessionId} '{Session.Caption}'";
        }

        public static SessionsTestCase[] GetTestCases()
        {
            S5ApiClient client = S5ApiClientFactory.Create();
            var sessions = client.SessionsAsync().GetSafeResult();
            var ret = sessions.Select(x => new SessionsTestCase() { Session = x });
            return ret.Concat(new List<SessionsTestCase>() { new SessionsTestCase() { Session = null } }).ToArray();
        }
    }
}
