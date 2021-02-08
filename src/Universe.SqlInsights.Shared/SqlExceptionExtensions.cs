using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Universe.SqlInsights.Shared
{
    public static class SqlExceptionExtensions
    {
        public static SqlException GetSqlError(this Exception ex)
        {
            return ex?.AsPlainExceptionList()
                .OfType<SqlException>()
                .FirstOrDefault();
        }
        
        public static IEnumerable<Exception> AsPlainExceptionList(this Exception ex)
        {
            while (ex != null)
            {
                if (ex is AggregateException ae)
                {
                    foreach (var subException in ae.Flatten().InnerExceptions)
                    {
                        yield return subException;
                    }
                    yield break;
                }

                yield return ex;
                ex = ex.InnerException;
            }
        }

        public static string GetBriefExceptionKey(this Exception ex)
        {
            StringBuilder ret = new StringBuilder();
            var list = AsPlainExceptionList(ex).Where(x => x?.GetType().Name != "HttpUnhandledException");
            foreach (var exception in list)
            {
                if (ret.Length > 0) ret.Append(" → ");
                SqlException sqlEx = exception as SqlException;
                ret.Append($"❰{exception.GetType().Name}{(sqlEx == null ? "" : " " + sqlEx.Number)}❱ {exception.Message}");
            }

            return ret.ToString();
        }

    }
}