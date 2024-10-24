using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;

namespace ErgoFab.DataAccess.IntegrationTests.Library;

public static class IDbConnectionStringExtensions
{
    public static ErgoFabDbContext CreateErgoFabDbContext(this IDbConnectionString dbConnectionString)
    {
        DbContextOptionsBuilder<ErgoFabDbContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<ErgoFabDbContext>();
        dbContextOptionsBuilder.UseSqlServer(dbConnectionString.ConnectionString, b => b.UseCompatibilityLevel(120));

        return new ErgoFabDbContext(dbContextOptionsBuilder.Options);
    }
}