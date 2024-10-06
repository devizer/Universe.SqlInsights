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
#if NETSTANDARD || NET5_0
        
        // IEnumerable<string> optionalApps = null, IEnumerable<string> optionalHosts = null
        private OptionalParametersInfo BuildOptionalParameters(
            StringsStorage strings,
            IEnumerable<string> optionalApps,
            IEnumerable<string> optionalHosts
        )
        {
            StringBuilder sqlWhere = new StringBuilder();
            var sqlParams = new DynamicParameters();
            if (optionalApps != null)
            {
                int appIndex = 0;
                foreach (var optionalApp in optionalApps)
                {
                    long? idAppName = strings.AcquireString(StringKind.AppName, optionalApp);
                    sqlParams.Add($"App{++appIndex}", idAppName.Value);
                }
                if (appIndex > 0)
                    sqlWhere.Append($" And AppName In ({string.Join(",",Enumerable.Range(1, appIndex).Select(x => $"@App{x}"))})");
            }

            if (optionalHosts != null)
            {
                int hostIndex = 0;
                foreach (var optionalHost in optionalHosts)
                {
                    long? idHost = strings.AcquireString(StringKind.HostId, optionalHost);
                    sqlParams.Add($"Host{++hostIndex}", idHost.Value);
                }
                if (hostIndex > 0)
                    sqlWhere.Append($" And HostId In ({string.Join(",",Enumerable.Range(1, hostIndex).Select(x => $"@Host{x}"))})");
            }
            
            return new OptionalParametersInfo()
            {
                Parameters = sqlParams,
                SqlWhere = sqlWhere,
            };
        }

        private OptionalParametersInfo BuildOptionalParameters_Kill_It_for_Single_Only_App_or_Host(StringsStorage strings, string optionalApp = null, string optionalHost = null)
        {
            StringBuilder sqlWhere = new StringBuilder();
            var sqlParams = new DynamicParameters();
            if (optionalApp != null)
            {
                long? idAppName = strings.AcquireString(StringKind.AppName, optionalApp);
                sqlParams.Add("AppName", idAppName.Value);
                sqlWhere.Append(" And AppName = @AppName");
            }
            if (optionalHost != null)
            {
                long? idHost = strings.AcquireString(StringKind.HostId, optionalHost);
                sqlParams.Add("HostId", idHost.Value);
                sqlWhere.Append(" And HostId = @HostId");
            }
            
            return new OptionalParametersInfo()
            {
                Parameters = sqlParams,
                SqlWhere = sqlWhere,
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