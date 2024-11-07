using System.DirectoryServices.ActiveDirectory;
using Universe.SqlInsights.NUnit;

namespace ErgoFab.DataAccess.IntegrationTests.Shared
{
    public class ErgoFabTestCase
    {
        // TODO: See BuildDatabase comments
        public IDbConnectionString ConnectionOptions { get; set; }
        public string Kind { get; set; }

        public override string ToString()
        {
            return $"{Kind} for {ConnectionOptions}";
        }
    }
}


