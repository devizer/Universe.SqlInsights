using System.Linq;
using Universe.SqlInsights.Shared;
using Universe.SqlTrace;

namespace Universe.SqlInsights.SqlServerStorage
{
    public class SummaryDataRow
    {
        public string KeyPath { get; set; }
        public long AppName { get; set; }
        public long HostId { get; set; }
        public long IdSession { get; set; }
        public long Version { get; set; }

        // Prev: Data
        public long Count { get; set; }
        public long ErrorsCount { get; set; }
        // public long ResponseLength { get; set; }

        public double AppDuration { get; set; }
        public double AppDurationSquared { get; set; }
        public double AppKernelUsage { get; set; }
        public double AppKernelUsageSquared { get; set; }
        public double AppUserUsage { get; set; }
        public double AppUserUsageSquared { get; set; }

        // SqlCounters
        public long SqlDuration { get; set; }
        public long SqlDurationSquared { get; set; }
        public long SqlCPU { get; set; }
        public long SqlCPUSquared { get; set; }
        public long SqlReads { get; set; }
        public long SqlReadsSquared { get; set; }
        public long SqlWrites { get; set; }
        public long SqlWritesSquared { get; set; }
        public long SqlRowCounts { get; set; }
        public long SqlRowCountsSquared { get; set; }
        public long SqlRequests { get; set; }
        public long SqlRequestsSquared { get; set; }
        public long SqlErrors { get; set; }
        public long SqlErrorsSquared { get; set; }

        public ActionSummaryCounters ToActionSummaryCounters()
        {
            return new ActionSummaryCounters()
            {
                Key = new SqlInsightsActionKeyPath(KeyPath.Split('→').Select(x => x.Trim()).ToArray()),
                AppDuration = AppDuration,
                AppKernelUsage = AppKernelUsage,
                AppUserUsage = AppUserUsage,
                SqlErrors = SqlErrors,
                Count = Count,
                RequestErrors = ErrorsCount,
                ResponseLength = 0,
                SqlCounters = new SqlCounters()
                {
                    Duration = SqlDuration,
                    CPU = SqlCPU,
                    Reads = SqlReads,
                    Writes = SqlWrites,
                    RowCounts = SqlRowCounts,
                    Requests = SqlRequests,
                }
            };
        }
    }
}