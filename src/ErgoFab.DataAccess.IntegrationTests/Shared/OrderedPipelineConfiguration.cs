﻿using Library.Implementation;
using NUnit.Framework.Interfaces;
using Universe.NUnitPipeline;


// Single shared configuration for all the test assemblies
public class OrderedPipelineConfiguration
{
    public static void Configure()
    {
        TempDebug.WriteLine("OrderedPipelineConfiguration.Configure()");
        NUnitPipelineChain.InternalReportFile = Path.Combine("TestsOutput", $"ErgoFab.DataAccess.IntegrationTests");

        NUnitPipelineChain.OnStart = new List<NUnitPipelineChainAction>()
        {
            new() { Title = CpuUsageInterceptor.Title, Action = CpuUsageInterceptor.OnStart },
            new() { Title = CpuUsageVizInterceptor.Title, Action = CpuUsageVizInterceptor.OnStart },
            new() { Title = DbTestPipeline.Title, Action = DbTestPipeline.OnStart }
        };

        NUnitPipelineChain.OnEnd = new List<NUnitPipelineChainAction>()
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
        TempDebug.WriteLine($"[OrderedPipelineConfiguration.MyGlobalFinish] Finish Type='{test.TestType}' for Test='{test.Name}' on NUnitPipeline");
    }

}