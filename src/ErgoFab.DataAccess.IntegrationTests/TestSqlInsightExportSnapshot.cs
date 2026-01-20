using System.Diagnostics.Metrics;
using System.IO.Compression;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using System.Data.SqlClient;
using Shared.TestDatabaseDefinitions;
using Universe.NUnitPipeline;
using Universe.SqlInsights.Shared;
using Universe.SqlInsights.SqlServerStorage;

namespace ErgoFab.DataAccess.IntegrationTests;

[NUnitPipelineAction]
[TempTestAssemblyAction]
public class TestSqlInsightExportSnapshot
{
    [Test]
    [TestCase(CompressionLevel.Fastest)]
    [TestCase(CompressionLevel.Optimal)]
    [TestCase(CompressionLevel.NoCompression)]
    [TestCase(CompressionLevel.SmallestSize)]
    public void ExportToFile(CompressionLevel compressionLevel)
    {
        var fileName = Path.Combine("TestsOutput", $"Storage Snapshot ({compressionLevel})" + ".zip");
        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
        using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 65536);
        ExportImplementation(stream, compressionLevel);
    }


    [Test]
    public void ExportToNullStream()
    {
        ExportImplementation(Stream.Null, CompressionLevel.NoCompression);
    }


    private static void ExportImplementation(Stream outputStream, CompressionLevel compressionLevel)
    {
        ISqlInsightsStorage storage = NUnitPipelineConfiguration.GetService<ISqlInsightsStorage>();
        // SqlServerSqlInsightsStorage storage = new SqlServerSqlInsightsStorage(SqlClientFactory.Instance, testCase.ConnectionOptions.ConnectionString);
        SqlInsightsExportImport export = new SqlInsightsExportImport(storage)
        {
            BufferSize = 16384,
            CompressionLevel = compressionLevel
        };

        var cs = (storage as SqlServerSqlInsightsStorage)?.ConnectionString;
        // Console.WriteLine($"[DEBUG ConnectionString] STARTING EXPORT '{cs}'");
        export.Export(outputStream).ConfigureAwait(false).GetAwaiter().GetResult();
        Console.WriteLine(export.Log);
    }
}