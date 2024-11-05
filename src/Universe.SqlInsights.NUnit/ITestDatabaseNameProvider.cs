using System.Threading.Tasks;

namespace ErgoFab.DataAccess.IntegrationTests.Library
{
    public interface ITestDatabaseNameProvider
    {
        Task<string> GetNextTestDatabaseName();
    }
}