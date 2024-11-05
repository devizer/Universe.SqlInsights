using NUnit.Framework;

namespace Universe.SqlInsights.NUnit
{
    // TODO: Remove it
    public class DbTestCaseSource : TestCaseSourceAttribute
    {
        protected DbTestCaseSource(string sourceName) : base(sourceName)
        {
        }
    }
}
