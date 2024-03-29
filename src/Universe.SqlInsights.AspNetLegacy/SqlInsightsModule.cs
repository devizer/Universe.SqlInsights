﻿using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using Universe.SqlInsights.Shared;
using Universe.SqlInsights.SqlServerStorage;
using Universe.SqlTrace;

namespace Universe.SqlInsights.AspNetLegacy
{
    public static class SqlInsightsModule
    {
        public static void BindSqlInsights(this HttpApplication app, ISqlInsightsConfiguration sqlInsightsConfiguration)
        {
            LegacySqlProfiler.SqlInsightsConfiguration =
                sqlInsightsConfiguration ?? throw new ArgumentNullException(nameof(sqlInsightsConfiguration));

            app.BeginRequest += (sender, args) =>
            {

                LegacySqlProfilerContext.Instance = new LegacySqlProfilerContext();
                var actionId = LegacySqlProfilerContext.Instance.ActionId = Guid.NewGuid().ToString("N");
                
                var hcs = LegacySqlProfiler.SqlInsightsConfiguration.HistoryConnectionString;
                if (hcs != null)
                {
                    var history = new SqlServerSqlInsightsStorage(SqlClientFactory.Instance, hcs);
                    if (!history.AnyAliveSession()) return;
                }

                
                if (sqlInsightsConfiguration.MeasureSqlMetrics)
                {
                    var appName = string.Format(sqlInsightsConfiguration.SqlClientAppNameFormat, actionId);
                    app.Context.Items["Profiler App Name"] = appName;
                    if (!Directory.Exists(sqlInsightsConfiguration.SqlTracesDirectory))
                        Directory.CreateDirectory(sqlInsightsConfiguration.SqlTracesDirectory);

                    var master = sqlInsightsConfiguration.ConnectionString;
                    SqlTraceReader traceReader = new SqlTraceReader();
                    traceReader.MaxFileSize = sqlInsightsConfiguration.MaxTraceFileSizeKb;
                    TraceRowFilter appFilter = TraceRowFilter.CreateByApplication(appName);
                    traceReader.Start(master, 
                        sqlInsightsConfiguration.SqlTracesDirectory, 
                        TraceColumns.Application | TraceColumns.Sql,
                        appFilter);

                    app.Context.Items["Sql Trace Reader"] = traceReader;
                }
                
                app.Context.Items["Advanced Stopwatch"] = AdvStopwatch.StartNew();

            };

            app.PostMapRequestHandler += (sender, args) =>
            {
            };

            app.Error += (sender, args) =>
            {
                Exception lastError = app.Server.GetLastError();
                SqlExceptionInfo sqlException = lastError.FindSqlError();
                var sqlInfo = sqlException == null ? "" : $" SQL ERROR #{sqlException.Number} {sqlException.Message}";
                var entryName = Assembly.GetEntryAssembly()?.Location;
                if (entryName != null) entryName = Path.GetFileName(entryName);
                Debug.WriteLine($"HttpApplication.Error from '{entryName}' {sqlInfo}: {Environment.NewLine}{lastError}");
                app.Context.Items["App Exception"] = lastError;
            };
            
            app.PostLogRequest += (sender, args) =>
            {
                var actionKeyPath = LegacySqlProfilerContext.Instance?.ActionKeyPath;
                Debug.WriteLine($"app.PostLogRequest for {actionKeyPath}");
                AttachMeasurementOfResponseLength(app, responseLength =>
                {
                    try
                    {
                        Debug.WriteLine(
                            $"SqlInsightsReport.Instance.AddResponseLength({responseLength}) for {actionKeyPath}");
                        
                        SqlInsightsReport.Instance.AddResponseLength(actionKeyPath, responseLength);
                    }
                    catch (Exception eex)
                    {
                        var info1 = eex.ToString();
                    }
                });
            };

            
            app.EndRequest += (sender, args) =>
            {
                if (LegacySqlProfilerContext.Instance?.ActionKeyPath == null) return;

                AdvStopwatch stopwatch = (AdvStopwatch) app.Context.Items["Advanced Stopwatch"];
                Exception lastError = (Exception) app.Context.Items["App Exception"];
                AdvStopwatchResult swResult = stopwatch.GetOptionalResult();
                swResult ??= new AdvStopwatchResult(0, 0, 0);
                
                ActionSummaryCounters newLine = new ActionSummaryCounters()
                {
                    Key = LegacySqlProfilerContext.Instance.ActionKeyPath,
                    Count = 1,
                    RequestErrors = lastError == null ? 0 : 1,
                    AppDuration = swResult.Time,
                    AppKernelUsage = swResult.KernelUsage,
                    AppUserUsage = swResult.UserUsage,
                };
                
                SqlTraceReader traceReader = (SqlTraceReader) app.Context.Items["Sql Trace Reader"];
                TraceDetailsReport details = null;
                if (traceReader != null)
                {
                    details = traceReader.ReadDetailsReport();
                    var sqlSummary = details.Summary;
                    Debug.WriteLine($@"On EndRequest for «{LegacySqlProfilerContext.Instance.ActionKeyPath}»:
{((char)160)} Sql Side Details (columns are '{details.IncludedColumns}'): {sqlSummary}
{((char)160)} App Side details: {swResult.ToHumanString()}");

                    newLine.SqlCounters = sqlSummary;
                    newLine.SqlErrors = details.Count(x => x.SqlErrorCode.HasValue);
                    traceReader.Stop();
                    traceReader.Dispose();

                    sqlInsightsConfiguration.DeleteTraceFile(traceReader.TraceFile);
                }
                else
                {
                    return;
                }

                ActionDetailsWithCounters actionDetails = new ActionDetailsWithCounters()
                {
                    AppName = sqlInsightsConfiguration.AppName,
                    HostId = sqlInsightsConfiguration.HostId,
                    Key = LegacySqlProfilerContext.Instance.ActionKeyPath,
                    At = DateTime.UtcNow,
                    IsOK = lastError == null,
                    AppDuration = newLine.AppDuration,
                    AppKernelUsage = newLine.AppKernelUsage,
                    AppUserUsage = newLine.AppUserUsage,
                };
                
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

                actionDetails.SqlStatements.AddRange(details.Select(x => new ActionDetailsWithCounters.SqlStatement()
                {
                    Counters = x.Counters,
                    Sql = x.Sql,
                    SpName = x.SpName,
                    SqlErrorCode = x.SqlErrorCode,
                    SqlErrorText = x.SqlErrorText,
                }));

                // TODO: Dump to file
                // newLine.Actions.AddLast(actionDetails);  

                if (LegacySqlProfiler.SqlInsightsConfiguration.Enabled)
                {
                    bool canSummarize = SqlInsightsReport.Instance.Add(actionDetails);

                    if (canSummarize)
                    {
                        var hcs = LegacySqlProfiler.SqlInsightsConfiguration.HistoryConnectionString;
                        if (hcs != null)
                        {
                            var history = new SqlServerSqlInsightsStorage(SqlClientFactory.Instance, hcs);
                            history.AddAction(actionDetails);
                        }
                    }
                }

                LegacySqlProfilerContext.Instance.ActionId = null;
                LegacySqlProfilerContext.Instance.ActionKeyPath = null;
                LegacySqlProfilerContext.Instance = null;
            };

        }
        
        private static void AttachMeasurementOfResponseLength(HttpApplication app, Action<long> propagateResponseLength)
        {
            Debug.WriteLine($"Transfer-Encoding: <[{app.Response.Headers["Transfer-Encoding"]}]>");
            if (app.Response.Headers["Transfer-Encoding"] == "chunked")
            {
                return;
            }

            Stream prev = app.Response.Filter;
            // Log.Info("Prev filter: " + prev);
            StreamWithCounters next = new StreamWithCounters(prev);
            next.StreamClosed += () =>
            {
                propagateResponseLength(next.TotalWrittenBytes);
            };

            app.Response.Filter = next;
        }

    }
}