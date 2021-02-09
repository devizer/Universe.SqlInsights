using Microsoft.AspNetCore.Mvc.Filters;

namespace Universe.SqlInsights.NetCore
{
    public class CustomExceptionFilter : IExceptionFilter
    {
        private ExceptionHolder ErrorHolder;

        public CustomExceptionFilter(ExceptionHolder errorHolder)
        {
            ErrorHolder = errorHolder;
        }

        public void OnException(ExceptionContext context)
        {
            ErrorHolder.Error = context.Exception;
        }
    }
}