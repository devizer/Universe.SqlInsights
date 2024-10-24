using ErgoFab.DataAccess.IntegrationTests.Shared;

namespace ErgoFab.DataAccess.IntegrationTests.Library
{
    public interface IDbConnectionString
    {
        // Getter for test implementation
        string ConnectionString { get; }
    }
}
