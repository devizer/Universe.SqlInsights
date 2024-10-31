using Universe.NUnitPipeline;
namespace ErgoFab.DataAccess.IntegrationTests.Shared;


// Single shared configuration for all the test assemblies
public class OrderedPipelineConfiguration
{
    public static void Configure()
    {
        NUnitPipelineChain.InternalReportFile = Path.Combine("TestsOutput", $"ErgoFab.DataAccess.IntegrationTests");

        NUnitPipelineChain.OnStart = new List<NUnitPipelineChainAction>()
        {
            new() { Title = CpuUsageInterceptor.Title, Action = CpuUsageInterceptor.OnStart },
            new() { Title = CpuUsageVizInterceptor.Title, Action = CpuUsageVizInterceptor.OnStart },
        };

        NUnitPipelineChain.OnEnd = new List<NUnitPipelineChainAction>()
        {
            new() { Title = CpuUsageInterceptor.Title, Action = CpuUsageInterceptor.OnFinish },
            new() { Title = CpuUsageVizInterceptor.Title, Action = CpuUsageVizInterceptor.OnFinish },
            new() { Title = DisposeInterceptor.Title, Action = DisposeInterceptor.OnFinish },
            new() { Title = CpuUsageTreeReportInterceptor.Title, Action = CpuUsageTreeReportInterceptor.OnFinish },
        };
    }

}