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
    [ErgoFabTestCaseSource(42)]
    public void ExportToNullStream(ErgoFabTestCase testCase)
    {
        ISqlInsightsStorage storage = NUnitPipelineConfiguration.GetService<ISqlInsightsStorage>();
        // SqlServerSqlInsightsStorage storage = new SqlServerSqlInsightsStorage(SqlClientFactory.Instance, testCase.ConnectionOptions.ConnectionString);
        SqlInsightsExportImport export = new SqlInsightsExportImport(storage)
        {
            BufferSize = 1,
            CompressionLevel = CompressionLevel.NoCompression
        };

        var cs = (storage as SqlServerSqlInsightsStorage)?.ConnectionString;
        // Console.WriteLine($"[DEBUG ConnectionString] STARTING EXPORT '{cs}'");
        export.Export(Stream.Null).ConfigureAwait(false).GetAwaiter().GetResult();
        Console.WriteLine(export.Log);
    }
}