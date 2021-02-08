using System.Collections.Generic;
using System.Linq;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.AspNetLegacy
{
    public static class ProfilerReportExtensions
    {
        // returns summary by actions only, without latest actions
        public static IEnumerable<ActionSummaryCounters> AsActionsSummary(this SqlInsightsReport report)
        {
            return report.AsList().Select(x => new ActionSummaryCounters()
            {
                Count = x.Count,
                Key = x.Key,
                AppDuration = x.AppDuration,
                RequestErrors = x.RequestErrors,
                ResponseLength = x.ResponseLength,
                SqlCounters = x.SqlCounters,
                SqlErrors = x.SqlErrors,
                AppKernelUsage = x.AppKernelUsage,
                AppUserUsage = x.AppUserUsage,
            });
        }
    }
}