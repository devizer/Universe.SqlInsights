using System;
using System.Data;
using System.Linq;
using Universe.SqlInsights.Shared;
using Universe.SqlTrace;

namespace Universe.SqlInsights.GenericInterceptor
{
    public class SqlGenericInterceptor
    {


        public static void PersistAction(
            ISqlInsightsConfiguration configuration,
            ISqlInsightsStorage storage,
            ICrossPlatformLogger crossPlatformLogger,
            SqlInsightsActionKeyPath keyPath,
            double durationMilliseconds,
            CpuUsage.CpuUsage totalCpuUsage,
            Exception caughtException,
            TraceDetailsReport details,
            SqlInsightsReport summaryReport
        )
        {
            try
            {
                Exception lastError = caughtException;
                var config = configuration;

                ActionDetailsWithCounters actionDetails = new ActionDetailsWithCounters()
                {
                    AppName = config.AppName,
                    HostId = config.HostId,
                    Key = keyPath,
                    At = DateTime.UtcNow,
                    IsOK = lastError == null,
                    AppDuration = durationMilliseconds,
                    AppKernelUsage = totalCpuUsage.KernelUsage.TotalMicroSeconds / 1000L,
                    AppUserUsage = totalCpuUsage.UserUsage.TotalMicroSeconds / 1000L,
                };

                actionDetails.SqlStatements.AddRange(details.Select(x =>
                    new ActionDetailsWithCounters.SqlStatement()
                    {
                        Counters = x.Counters,
                        Sql = x.Sql,
                        SpName = x.SpName,
                        SqlErrorCode = x.SqlErrorCode,
                        SqlErrorText = x.SqlErrorText,
                    }));

                // ERROR
                SqlExceptionInfo sqlException = lastError.FindSqlError();
                if (sqlException != null)
                {
                    actionDetails.BriefSqlError = new BriefSqlError()
                    {
                        Message = sqlException.Message,
                        SqlErrorCode = sqlException.Number,
                    };
                }

                if (lastError != null)
                {
                    actionDetails.ExceptionAsString = lastError.ToString();
                    actionDetails.BriefException = lastError.GetBriefExceptionKey();
                }

                
                SqlInsightsReport inMemoryReport = summaryReport;
                bool canSummarize = inMemoryReport.Add(actionDetails);

                if (canSummarize) // not a first call?
                {
                    if (storage is ITraceableStorage traceableStorage)
                    {
                        ExperimentalMeasuredAction.Perform(
                            config,
                            new SqlInsightsActionKeyPath($"[{storage.GetType().Name}]", "AddAction()"),
                            connectionString =>
                            {
                                traceableStorage.ConnectionString = connectionString;
                                storage?.AddAction(actionDetails);
                            },
                            crossPlatformLogger
                        );
                    }
                    else
                    {
                        storage?.AddAction(actionDetails);
                    }
                }

                // TODO: Implement 'isPoolingOn' (cached) and 'dbConnection' (opened?)   
                bool isPoolingOn = false;
                if (isPoolingOn)
                {
                    IDbConnection dbConnection = null;
#if HAS_MICROSOFT_DATA_SQLCLIENT
                    if (dbConnection is Microsoft.Data.SqlClient.SqlConnection msDbConnection)
                        Microsoft.Data.SqlClient.SqlConnection.ClearPool(msDbConnection);
                    else
#endif
                    if (dbConnection is System.Data.SqlClient.SqlConnection systemDbConnection)
                        System.Data.SqlClient.SqlConnection.ClearPool(systemDbConnection);
                }

                // Console.WriteLine($"⚠ Processed  {aboutRequest}");
            }
            catch (Exception ex)
            {
                // TODO: (Error 2627 on AddAction)
                // System.Data.SqlClient.SqlException (0x80131904): Violation of PRIMARY KEY constraint 'PK_SqlInsightsKeyPathSummary'. Cannot insert duplicate key in object 'dbo.SqlInsightsKeyPathSummary'. The duplicate key value is (Misc→/api/v1/SqlInsights/Summary→[OPTIONS], 0, 1, 3).
                Console.WriteLine($"[SqlInsights Storage] ERROR storing details on '{keyPath}'. Misconfigured." + Environment.NewLine + ex);
                throw new InvalidOperationException($"[SqlInsights Storage] ERROR storing details on '{keyPath}'. Misconfigured.", ex);
            }

        }


    }
}