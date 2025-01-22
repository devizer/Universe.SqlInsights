using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Slices.Dashboard;
using static System.Collections.Specialized.BitVector32;

namespace Slices.Dashboard
{
    public partial class SqlInsightsSession
    {
        /*[JsonIgnore]*/
        public DateTimeOffset? CalculatedEnding { get; set; }
        /*[JsonIgnore]*/
        public DateTimeOffset? ExpiringDate { get; set; }

        public bool CalculatedIsFinished { get; set; }
    }

}

namespace Universe.SqlInsights.W3Api.Client.GeneratedClient
{
    public static class SessionsExtensions
    {
        // Done: Moved to desktop client
        public static void PopulateSessionsCalculatedFields(this IEnumerable<SqlInsightsSession> sessions)
        {
            var localTimeZone = TimeZoneInfo.Local;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            foreach (SqlInsightsSession s in sessions)
            {
                if (s.MaxDurationMinutes.HasValue)
                    s.ExpiringDate = s.StartedAt + TimeSpan.FromMinutes(s.MaxDurationMinutes.Value);

                if (s.EndedAt.HasValue)
                    s.CalculatedEnding = s.EndedAt;

                if (!s.CalculatedEnding.HasValue && s.ExpiringDate.HasValue)
                {
                    s.CalculatedEnding = s.ExpiringDate;
                }

                //// Does Not work at evening. lol
                //DateTimeOffset now = DateTimeOffset.Now.Subtract(TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
                //if (s.CalculatedEnding.HasValue && now >= s.CalculatedEnding.Value)
                //    s.CalculatedIsFinished = true;
                if (s.CalculatedEnding.HasValue)
                {
                    var nowUtcDateTime = now.UtcDateTime;
                    var calculatedEndingUtcDateTime = s.CalculatedEnding.Value.UtcDateTime;
                    var calculatedEndingUtcDateTimeCorrected = calculatedEndingUtcDateTime.Add(localTimeZone.GetUtcOffset(calculatedEndingUtcDateTime));

                    if (nowUtcDateTime >= calculatedEndingUtcDateTimeCorrected)
                        s.CalculatedIsFinished = true;
                }


            }
        }

        public static void PopulateSessionsCalculatedFields_Incorrect(this IEnumerable<SqlInsightsSession> sessions)
        {
            foreach (var s in sessions)
            {
                if (!s.EndedAt.HasValue)
                {
                    if (s.MaxDurationMinutes.HasValue)
                    {
                        s.EndedAt = s.StartedAt + TimeSpan.FromMinutes(s.MaxDurationMinutes.Value);
                        s.IsFinished = DateTimeOffset.UtcNow >= s.EndedAt;
                    }
                }
            }
        }
    }
}
