﻿using ErgoFab.DataAccess.IntegrationTests.Shared;
using NUnit.Framework.Interfaces;
using Universe.NUnitPipeline;
using Universe.SqlInsights.NUnit;


// Single shared configuration for all the test assemblies
public class OrderedPipelineConfiguration
{
    public static void Configure()
    {
        PipelineLog.LogTrace($"[{typeof(OrderedPipelineConfiguration)}]::Configure()");

        var reportConfiguration = NUnitPipelineConfiguration.GetService<NUnitReportConfiguration>();
        reportConfiguration.InternalReportFile = Path.Combine("TestsOutput", $"ErgoFab.DataAccess.IntegrationTests");

        NUnitPipelineConfiguration.Register<ITestDatabaseNameProvider>(() => new TestDatabaseNameProvider());
        NUnitPipelineConfiguration.Register<SqlServerTestDbManager>(() => new SqlServerTestDbManager(SqlServerTestsConfiguration.Instance));
        NUnitPipelineConfiguration.Register<ISqlServerTestsConfiguration>(() => SqlServerTestsConfiguration.Instance);
        

        var chain = NUnitPipelineConfiguration.GetService<NUnitPipelineChain>();

        chain.OnStart = new List<NUnitPipelineChainAction>()
        {
            new() { Title = CpuUsageInterceptor.Title, Action = CpuUsageInterceptor.OnStart },
            new() { Title = CpuUsageVizInterceptor.Title, Action = CpuUsageVizInterceptor.OnStart },
            new() { Title = DbTestPipeline.Title, Action = DbTestPipeline.OnStart },
        };

        chain.OnEnd = new List<NUnitPipelineChainAction>()
        {
            new() { Title = CpuUsageInterceptor.Title, Action = CpuUsageInterceptor.OnFinish },
            new() { Title = CpuUsageVizInterceptor.Title, Action = CpuUsageVizInterceptor.OnFinish },
            new() { Title = DbTestPipeline.Title, Action = DbTestPipeline.OnFinish },
            new() { Title = DisposeInterceptor.Title, Action = DisposeInterceptor.OnFinish },
            new() { Title = CpuUsageTreeReportInterceptor.Title, Action = CpuUsageTreeReportInterceptor.OnFinish },
            new() { Title = "Debugger Global Finish", Action = MyGlobalFinish },
        };
    }


    static void MyGlobalFinish(NUnitStage stage, ITest test)
    {
        PipelineLog.LogTrace($"[OrderedPipelineConfiguration.MyGlobalFinish] Finish Type='{test.TestType}' for Test='{test.Name}' on NUnitPipeline");
    }

}