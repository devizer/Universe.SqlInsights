using NUnit.Framework;

namespace Universe.NUnitPipeline.SqlServerDatabaseFactory
{
    // TODO: Remove it
    public class DbTestCaseSource : TestCaseSourceAttribute
    {
        protected DbTestCaseSource(string sourceName) : base(sourceName)
        {
        }
    }
}
