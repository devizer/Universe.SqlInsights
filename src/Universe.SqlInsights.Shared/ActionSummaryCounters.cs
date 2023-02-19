using Universe.SqlTrace;

namespace Universe.SqlInsights.Shared
{
    // Table SqlInsights KeyPathSummary
    public class ActionSummaryCounters
    {
        public SqlInsightsActionKeyPath Key { get; set; }
        public long Count { get; set; }
        public long RequestErrors { get; set; }
        public long ResponseLength { get; set; }

        // Total
        public double AppDuration { get; set; }
        public double AppKernelUsage { get; set; }
        public double AppUserUsage { get; set; }
        public SqlCounters SqlCounters { get; set; }
        public long SqlErrors { get; set; }

        public void Add(ActionSummaryCounters other)
        {
            this.Count += other.Count;
            this.RequestErrors += other.RequestErrors;
            this.ResponseLength += other.ResponseLength;
            this.AppDuration += other.AppDuration;
            this.AppKernelUsage += other.AppKernelUsage;
            this.AppUserUsage += other.AppUserUsage;
            this.SqlCounters = this.SqlCounters.Add(other.SqlCounters);
            this.SqlErrors += other.SqlErrors;
        }
    }
}