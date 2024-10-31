
namespace ErgoFab.DataAccess.IntegrationTests.Library
{
    // TODO: Move to DataAccess assembly. Inject into DI on Main/Startup
    public interface IDbConnectionString
    {
        // Getter for test implementation
        string ConnectionString { get; }
    }
}
