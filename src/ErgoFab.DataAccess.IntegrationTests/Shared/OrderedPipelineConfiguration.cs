using System.Reflection;
using System.Runtime.Versioning;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using NUnit.Framework.Interfaces;
using Tracking;
using Universe.NUnitPipeline;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;
using Universe.SqlInsights.NUnit;
using Universe.SqlInsights.Shared;
using Universe.SqlInsights.SqlServerStorage;


// Single shared configuration for all the test assemblies
public class OrderedPipelineConfiguration
{
    public static void Configure()
    {
        PipelineLog.LogTrace($"[{typeof(OrderedPipelineConfiguration)}]::Configure()");

        var reportConfiguration = NUnitPipelineConfiguration.GetService<NUnitReportConfiguration>();
        reportConfiguration.InternalReportFile = Path.Combine("TestsOutput", $"ErgoFab.DataAccess.IntegrationTests {GetCurrentNetVersion()}");

        NUnitPipelineConfiguration.Register<ITestDatabaseNameProvider>(() => new TestDatabaseNameProvider());
        NUnitPipelineConfiguration.Register<SqlServerTestDbManager>(() => new SqlServerTestDbManager(SqlServerTestsConfiguration.Instance));
        NUnitPipelineConfiguration.Register<ISqlServerTestsConfiguration>(() => SqlServerTestsConfiguration.Instance);
        
        NUnitPipelineConfiguration.Register<ISqlInsightsConfiguration>(() => new ErgoFabSqlInsightsConfiguration());
        NUnitPipelineConfiguration.Register<ISqlInsightsStorage>(() =>
        {
            var ret = new SqlServerSqlInsightsStorage(
                NUnitPipelineConfiguration.GetService<SqlServerTestDbManager>().CreateDbProviderFactory(),
                NUnitPipelineConfiguration.GetService<ISqlInsightsConfiguration>().HistoryConnectionString
            );

            // Migrate
            ret.AnyAliveSession();
            return ret;
        });
        

        var chain = NUnitPipelineConfiguration.GetService<NUnitPipelineChain>();

        chain.OnStart = new List<NUnitPipelineChainAction>()
        {
            new() { Title = CpuUsageInterceptor.Title, Action = CpuUsageInterceptor.OnStart },
            new() { Title = CpuUsageVizInterceptor.Title, Action = CpuUsageVizInterceptor.OnStart },
            new() { Title = DbTestPipeline.Title, Action = DbTestPipeline.OnStart },
            new() { Title = SqlServerTestCapturePipeline.Title, Action = SqlServerTestCapturePipeline.OnStart },
        };

        chain.OnEnd = new List<NUnitPipelineChainAction>()
        {
            new() { Title = CpuUsageInterceptor.Title, Action = CpuUsageInterceptor.OnFinish },
            new() { Title = SqlServerTestCapturePipeline.Title, Action = SqlServerTestCapturePipeline.OnFinish },
            new() { Title = CpuUsageVizInterceptor.Title, Action = CpuUsageVizInterceptor.OnFinish },
            new() { Title = DbTestPipeline.Title, Action = DbTestPipeline.OnFinish },
            new() { Title = DisposeInterceptor.Title, Action = DisposeInterceptor.OnFinish },
            new() { Title = CpuUsageTreeReportInterceptor.Title, Action = CpuUsageTreeReportInterceptor.OnFinish },
            new() { Title = "Debugger Global Finish", Action = MyGlobalFinish },
        };
    }

    private static string GetCurrentNetVersion()
    {
        var targetFramework = Assembly.GetExecutingAssembly()
            .GetCustomAttributes(typeof(TargetFrameworkAttribute))
            .OfType<TargetFrameworkAttribute>()
            .FirstOrDefault()
            ?.FrameworkName;

        var ver = targetFramework?.Split('=')?.Last();

        return ver == null ? "" : $" NET {ver}";
    }


    static void MyGlobalFinish(NUnitStage stage, ITest test)
    {
        PipelineLog.LogTrace($"[OrderedPipelineConfiguration.MyGlobalFinish] Finish Type='{test.TestType}' for Test='{test.Name}' on NUnitPipeline");
    }

}