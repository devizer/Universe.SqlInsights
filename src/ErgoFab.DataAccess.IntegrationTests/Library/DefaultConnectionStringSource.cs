using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErgoFab.DataAccess.IntegrationTests.Library;

namespace ErgoFab.DataAccess.IntegrationTests.Shared
{
    public class DefaultConnectionStringSource
    {
        public IDbConnectionString ConnectionOptions { get; set; }
    }
}
