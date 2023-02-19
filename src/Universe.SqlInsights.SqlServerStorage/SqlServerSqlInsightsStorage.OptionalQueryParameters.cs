using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage
{
    public partial class SqlServerSqlInsightsStorage 
    {
#if NETSTANDARD
        
        private OptionalParametersInfo BuildOptionalParameterts(StringsStorage strings, string optionalApp = null, string optionalHost = null)
        {
            StringBuilder sql = new StringBuilder();
            var sqlParams = new DynamicParameters();
            if (optionalApp != null)
            {
                long? idAppName = strings.AcquireString(StringKind.AppName, optionalApp);
                sqlParams.Add("AppName", idAppName.Value);
                sql.Append(" And AppName = @AppName");
            }
            if (optionalHost != null)
            {
                long? idHost = strings.AcquireString(StringKind.HostId, optionalHost);
                sqlParams.Add("HostId", idHost.Value);
                sql.Append(" And HostId = @HostId");
            }
            
            return new OptionalParametersInfo()
            {
                Parameters = sqlParams,
                SqlWhere = sql,
            };
        }

        struct OptionalParametersInfo
        {
            public StringBuilder SqlWhere;
            public DynamicParameters Parameters;
        }
        

#endif        
    }
}