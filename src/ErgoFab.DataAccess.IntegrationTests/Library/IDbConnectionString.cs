using ErgoFab.DataAccess.IntegrationTests.Shared;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;

namespace ErgoFab.DataAccess.IntegrationTests.Library
{
    public interface IDbConnectionString
    {
        // Getter for test implementation
        string ConnectionString { get; }
    }

    public static class IDbConnectionStringExtensions
    {
        public static ErgoFabDbContext CreateErgoFabDbContext(this IDbConnectionString dbConnectionString)
        {
            DbContextOptionsBuilder<ErgoFabDbContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<ErgoFabDbContext>();
            dbContextOptionsBuilder.UseSqlServer(dbConnectionString.ConnectionString);
            return new ErgoFabDbContext(dbContextOptionsBuilder.Options);
        }
    }
}
