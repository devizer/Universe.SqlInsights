using System.Data.Common;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Universe.NUnitPipeline;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;

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

    private static long DbContextCounter = 0;
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

        int logRowCounter = 0;
        long dbContextCounter = Interlocked.Increment(ref DbContextCounter);
        Action<string> log = s =>
        {
            logRowCounter++;
            Console.WriteLine($"# {dbContextCounter:0}.{logRowCounter:0}{Environment.NewLine}{s}");
        };
        Func<EventId, LogLevel, bool> logFilter = (EventId id, LogLevel level) =>
        {
            // return true;
            var events = new[] { "QueryCompilationStarting", "QueryExecutionPlanned", "CommandExecuted" };
            return events.Any(e => id.Name.Contains(e, StringComparison.OrdinalIgnoreCase));
        };
        
        dbContextOptionsBuilder.LogTo(log, logFilter).EnableDetailedErrors().EnableSensitiveDataLogging();

        var ergoFabDbContext = new ErgoFabDbContext(dbContextOptionsBuilder.Options);
        return ergoFabDbContext;
    }

    static void CheckConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("ConnectionString is null or empty. DB Test Pipeline is misconfigured");

    }

}