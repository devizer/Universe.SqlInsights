using System.Web;
using System.Web.Mvc;
using AdventureWorks.SqlInsightsIntegration;

namespace AdventureWorks
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new CustomGroupingActionFilter());
            filters.Add(new HandleErrorAttribute());
        }
    }
}
