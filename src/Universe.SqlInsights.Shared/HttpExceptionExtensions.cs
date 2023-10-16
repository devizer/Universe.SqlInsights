using System;
using System.Reflection;

namespace Universe.SqlInsights.Shared
{
    public class HttpExceptionExtensions
    {
        private Lazy<Type> _HttpExceptionType = new Lazy<Type>(CreateHttpExceptionType);
        private static readonly object[] EmptyObjectArray = new object[0];

        private static Type CreateHttpExceptionType()
        {
            Type type = Type.GetType("System.Web.HttpException", false);
            return type;
        }

        public static int? TryGetHttpExceptionStatus(Exception ex)
        {
            if (ex == null) return null;

            var type = ex.GetType();
            if (type.Name == "HttpException" || true)
            {

#if NETSTANDARD1_3
                var property = type.GetTypeInfo().GetDeclaredProperty("WebEventCode");
#else
                PropertyInfo property = type.GetProperty("WebEventCode");
#endif
                if (property != null)
                {
                    var rawRet = property.GetValue(ex, EmptyObjectArray);
                    if (rawRet is int)
                    {
                        int ret = (int)rawRet;
                        if (ret > 0 && ret <= 32767) return ret;
                    }
                }
            }

            return null;
        }

    }
}
