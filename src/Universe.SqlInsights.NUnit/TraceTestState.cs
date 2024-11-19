using System.Diagnostics;
using Universe.CpuUsage;
using Universe.SqlTrace;

namespace Universe.SqlInsights.NUnit;

public class TraceTestState
{
    public string ActionId { get; set; }
    public SqlTraceReader TraceReader { get; set; }

    // Same as CpuUsageState
    public Stopwatch Stopwatch { get; set; }
    public CpuUsageAsyncWatcher CpuUsageAsyncWatcher { get; set; }
    public CpuUsage.CpuUsage? CpuUsageOnStart { get; set; }
    public int ThreadIdOnStart { get; set; }
    public bool Finished { get; set; } = false;
}