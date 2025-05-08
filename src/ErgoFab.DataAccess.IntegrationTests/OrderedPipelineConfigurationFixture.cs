using System.Diagnostics;
using System.IO.Compression;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using Universe.NUnitPipeline;
using Universe.SqlInsights.Shared;

[assembly: NUnitPipelineAction]
[assembly: TempTestAssemblyAction]

// One time setup fixture per each test project
[SetUpFixture]
public class OrderedPipelineConfigurationFixture
{
    [OneTimeSetUp]
    public void Configure()
    {
        OrderedPipelineConfiguration.Configure();
    }


    private static int Counter = 0;
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        var letsDebug = "ok";
        var storage = NUnitPipelineConfiguration.GetService<ISqlInsightsStorage>();
        SqlInsightsExportImport export = new SqlInsightsExportImport(storage)
        {
            BufferSize = 32768,
            CompressionLevel = CompressionLevel.Fastest
        };

        var counter = Interlocked.Increment(ref Counter);
        export.Export($"Storage Snapshot{Path.DirectorySeparatorChar}ErgoFab {counter}.zip");
    }
}

