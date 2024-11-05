using System.Threading.Tasks;

namespace Universe.SqlInsights.NUnit
{
    public interface ITestDatabaseNameProvider
    {
        Task<string> GetNextTestDatabaseName();
    }
}