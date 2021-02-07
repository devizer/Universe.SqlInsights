using System;
using System.Collections.Generic;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.AspNetLegacy
{
    public class SharedProfilerReport
    {
        public DateTime GeneratedAt { get; set; }
        public bool IsSqlMetricsIncluded { get; set; }
        public List<ActionSummaryCounters> Counters { get; set; }
    }
}