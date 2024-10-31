using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

public static class ErgoFabTestCaseExtensions
{
    public static ErgoFabDbContext CreateErgoFabDbContext(this ErgoFabTestCase ergoFabTestCase)
    {
        return CreateErgoFabDbContext(ergoFabTestCase.ConnectionOptions);
    }

    public static ErgoFabDbContext CreateErgoFabDbContext(this IDbConnectionString dbConnectionString)
    {
        DbContextOptionsBuilder<ErgoFabDbContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<ErgoFabDbContext>();
        dbContextOptionsBuilder.UseSqlServer(dbConnectionString.ConnectionString, b => b.UseCompatibilityLevel(120));
        return new ErgoFabDbContext(dbContextOptionsBuilder.Options);
    }

}