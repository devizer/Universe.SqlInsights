using System;
using System.Collections.Generic;
using System.Text;
using Slices.Dashboard;

namespace Universe.SqlInsights.W3Api.Client.GeneratedClient
{
    public static class SessionsExtensions
    {
        public static void PopulateSessionsCalculatedFields(this IEnumerable<SqlInsightsSession> sessions)
        {
            foreach (var s in sessions)
            {
                if (!s.EndedAt.HasValue)
                {
                    if (s.MaxDurationMinutes.HasValue)
                    {
                        s.EndedAt = s.StartedAt + TimeSpan.FromMinutes(s.MaxDurationMinutes.Value);
                        s.IsFinished = DateTimeOffset.Now >= s.EndedAt;
                    }
                }
            }
        }
    }
}
