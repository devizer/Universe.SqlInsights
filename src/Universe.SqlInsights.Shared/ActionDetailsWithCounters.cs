using System;
using System.Collections.Generic;
using Universe.SqlTrace;

namespace Universe.SqlInsights.Shared
{
    // Table SqlInsights Action
    public class ActionDetailsWithCounters
    {
        public string AppName { get; set; }
        public string HostId { get; set; }
        public SqlInsightsActionKeyPath Key { get; set; }
        public DateTime At { get; set; }
        public bool IsOK { get; set; }
        public string ExceptionAsString { get; set; }
        public string BriefException { get; set; }
        
        public BriefSqlError BriefSqlError { get; set; }
        
        // Same As ActionSummaryCounters
        public double AppDuration { get; set; }
        public double AppKernelUsage { get; set; }
        public double AppUserUsage { get; set; }

        public List<SqlStatement> SqlStatements { get; set; } = new List<SqlStatement>();

        public class SqlStatement
        {
            // Same as SqlStatementCounters
            public int? SqlErrorCode { get; set; }
            public string SpName { get; set; }
            public string Sql { get; set; }
            public SqlCounters Counters { get; set; }
            public string SqlErrorText { get; set; }
        }
    }

    public static class ActionDetailsWithCountersExtensions
    {
        public static ActionSummaryCounters AsSummary(this ActionDetailsWithCounters reqAction)
        {
            SqlCounters sqlCounters = SqlCounters.Zero;
            int sqlErrors = 0;
            if (reqAction.SqlStatements != null)
            {
                foreach (var statement in reqAction.SqlStatements)
                {
                    sqlCounters = sqlCounters.Add(statement.Counters);
                    if (statement.SqlErrorCode.HasValue) sqlErrors++;
                }
            }

            ActionSummaryCounters actionActionSummary = new ActionSummaryCounters()
            {
                Key = reqAction.Key,
                Count = 1,
                AppDuration = reqAction.AppDuration,
                RequestErrors = string.IsNullOrEmpty(reqAction.BriefException) ? 0 : 1,
                ResponseLength = 0,
                SqlCounters = sqlCounters,
                SqlErrors = sqlErrors,
                AppKernelUsage = reqAction.AppKernelUsage,
                AppUserUsage = reqAction.AppUserUsage,
            };

            return actionActionSummary;
        }
    }
    
    public class BriefSqlError
    {
        public int SqlErrorCode { get; set; }
        public string Message { get; set; }
    }

}