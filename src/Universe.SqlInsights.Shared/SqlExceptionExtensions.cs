﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Universe.SqlInsights.Shared
{
    // Supports both System and Microsoft sql client.
    public class SqlExceptionInfo
    {
        public int Number { get; set; }
        public string Message { get; set; }
    }
    
    public static class SqlExceptionExtensions
    {
        public static SqlExceptionInfo FindSqlError(this Exception ex)
        {
            return ex?.AsPlainExceptionList()
                .Select(IsSqlException)
                .FirstOrDefault(x => x != null);

        }

        public static SqlExceptionInfo IsSqlException(this Exception theException)
        {
            if (theException is System.Data.SqlClient.SqlException sysError) return new SqlExceptionInfo()
            {
                Message = sysError.Message,
                Number = sysError.Number
            };
            
#if NET461 || NETSTANDARD2_0
            if (theException is Microsoft.Data.SqlClient.SqlException msError)
            {
                return new SqlExceptionInfo()
                {
                    Message = msError.Message,
                    Number = msError.Number,
                };
            }
#endif
            return null;
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
                SqlExceptionInfo sqlEx = IsSqlException(exception);
                ret.Append($"❰{exception.GetType().Name}{(sqlEx == null ? "" : " " + sqlEx.Number)}❱ {exception.Message}");
            }

            return ret.ToString();
        }

    }
}