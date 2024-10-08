using System.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Universe.SqlInsights.W3Api.Controllers
{

    [ApiController]
    [Route("api/" + ApiVer.V1 + "/SqlInsights/About/[action]")]
    public class AboutController : ControllerBase
    {
        private DbOptions DbOptions;

        public AboutController(DbOptions dbOptions)
        {
            DbOptions = dbOptions;
        }

        [HttpPost]
        public async Task<ActionResult<AboutResponse>> Index()
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(DbOptions.ConnectionString);
            var ret = new AboutResponse()
            {
                ApiVersion = version?.ToString(),
                DbCatalog = b.InitialCatalog,
                DbServer = b.DataSource
            };

            return ret.ToJsonResult();
        }
    }

    public class AboutResponse
    {
        public string ApiVersion { get; set; }
        public string DbServer { get; set; }
        public string DbCatalog { get; set; }

    }

}