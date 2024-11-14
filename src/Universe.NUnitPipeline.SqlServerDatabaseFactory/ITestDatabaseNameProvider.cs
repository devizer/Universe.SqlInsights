using System.Threading.Tasks;

namespace Universe.NUnitPipeline.SqlServerDatabaseFactory
{
    public interface ITestDatabaseNameProvider
    {
        Task<string> GetNextTestDatabaseName();
    }
}