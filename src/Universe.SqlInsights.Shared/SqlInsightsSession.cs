using System;

namespace Universe.SqlInsights.Shared
{
    public class SqlInsightsSession
    {
        public long IdSession { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsFinished { get; set; }
        public string Caption { get; set; }
        public int? MaxDurationMinutes { get; set; }
    }
}