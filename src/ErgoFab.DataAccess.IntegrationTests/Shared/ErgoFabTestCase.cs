using ErgoFab.DataAccess.IntegrationTests.Library;

namespace ErgoFab.DataAccess.IntegrationTests.Shared
{
    public class ErgoFabTestCase
    {
        public IDbConnectionString ConnectionOptions { get; set; }
        public string Kind { get; set; }

        public override string ToString()
        {
            return $"{Kind} for {ConnectionOptions}";
        }
    }
}


