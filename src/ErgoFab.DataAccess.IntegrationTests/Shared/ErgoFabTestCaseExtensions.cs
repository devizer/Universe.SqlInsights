using System.Data.Common;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Universe.NUnitPipeline;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;
using Universe.SqlInsights.NUnit;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

public static class ErgoFabTestCaseExtensions
{
    public static ErgoFabDbContext CreateErgoFabDbContext(this ErgoFabTestCase ergoFabTestCase)
    {
        return CreateErgoFabDbContext(ergoFabTestCase.ConnectionOptions);
    }

    public static DbConnection CreateDbConnection(this ErgoFabTestCase ergoFabTestCase)
    {
        CheckConnectionString(ergoFabTestCase.ConnectionOptions.ConnectionString);
        var provider = new SqlServerTestDbManager(SqlServerTestsConfiguration.Instance).CreateDbProviderFactory();
        var connection = provider.CreateConnection();
        connection.ConnectionString = ergoFabTestCase.ConnectionOptions.ConnectionString;
        return connection;
    }

    public static ErgoFabDbContext CreateErgoFabDbContext(this IDbConnectionString dbConnectionString)
    {
        // TODO:
        // NO WAY
        // 1. Create DB using passed migration and and seeder
        // 2. START TRACE HERE 
        CheckConnectionString(dbConnectionString?.ConnectionString);

        DbContextOptionsBuilder<ErgoFabDbContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<ErgoFabDbContext>();
        PipelineLog.LogTrace($"[CreateErgoFabDbContext()] Using Connection String for ErgoFabDbContext: '{dbConnectionString.ConnectionString}'");
        dbContextOptionsBuilder.UseSqlServer(dbConnectionString.ConnectionString, b =>
        {
#if NET8_0_OR_GREATER
            b.UseCompatibilityLevel(120);
#endif
            b.EnableRetryOnFailure(5);
        });

        return new ErgoFabDbContext(dbContextOptionsBuilder.Options);
    }

    static void CheckConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("ConnectionString is null or empty. DB Test Pipeline is misconfigured");

    }

}