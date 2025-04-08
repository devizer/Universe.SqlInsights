using System;
using System.Data.SqlClient;
using AdventureWorks.SqlInsightsIntegration;
using AdventureWorks.Utils;

namespace AdventureWorks.WebPages
{
    public partial class FakeHealthCheck : System.Web.UI.Page
    {
            
        protected void Page_Load(object sender, EventArgs e)
        {
            using (SqlConnection con = new SqlConnection(ConnectionStringBuilder.BuildConnectionString()))
            {
                ErrorsSandbox.CheckHealth(con);
            }
        }

    }
}