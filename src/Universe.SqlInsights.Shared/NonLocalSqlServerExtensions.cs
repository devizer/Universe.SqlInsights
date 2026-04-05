using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;

namespace Universe.SqlInsights.Shared
{
    public static class NonLocalSqlServerExtensions
    {
        public static void DeleteTraceFile(this ISqlInsightsConfiguration config, string traceFile)
        {
            if (config.DisposeByShellCommand)
            {
                using (SqlConnection con = new SqlConnection(config.ConnectionString))
                {
                    // TODO: Replace REPLACE(@command_string, '/', '\') Hack by explicit tracefile arg
                    string shellCommand = $"del \"{traceFile}.trc\"";
                    string sql = @"
Declare @host_platform nvarchar(4000);
/*
If ((@@MICROSOFTVERSION / 16777216) <= 13) Set @host_platform = 'Windows' -- Version up to 2016
If ((@@MICROSOFTVERSION / 16777216) >= 14) -- Version 2017+
  -- And Exists (Select 1 From sys.all_objects Where [name] = 'dm_os_host_info' and [type] = 'V' and is_ms_shipped = 1)
  And OBJECT_ID('sys.dm_os_host_info', 'V') IS NOT NULL
Select @host_platform = host_platform from sys.dm_os_host_info;
*/
If ((@@MICROSOFTVERSION / 16777216) <= 13) Set @host_platform = 'Windows' Else Select @host_platform = host_platform from sys.dm_os_host_info;

If (@host_platform = 'Windows')
Begin
  DECLARE @result int;
  SET @command_string = REPLACE(@command_string, '/', '\');
  Exec @result = xp_cmdshell @command_string; 
  Select @Result;
End";
                    try
                    {
                        con.Open();
						// TODO: ONLY ON WINDOWS !!!!!!!!!!!!!!!!!!!!!!!
						int? xp_cmd_result = null;
						string sql_error_text = null;

						using (SqlCommand cmd = new SqlCommand(sql, con))
						{
							cmd.Parameters.Add("@command_string", SqlDbType.NVarChar, 4000).Value = shellCommand;
							object cmdResult = cmd.ExecuteScalar();
							if (cmdResult is int intValue)
							{
								xp_cmd_result = intValue;
								sql_error_text = null;
								LogForDebug($"[Debug] DeleteTraceFile cmdResult = {xp_cmd_result}. CMD IS [{shellCommand}]");
							}
							else if (cmdResult is string stringValue)
							{
								sql_error_text = stringValue;
								xp_cmd_result = null;
								LogForDebug($"[Debug] DeleteTraceFile cmdResult = '{sql_error_text}'. CMD IS [{shellCommand}]");
							}
							else if (cmdResult != null && cmdResult != DBNull.Value)
							{
								LogForDebug($"[Debug] DeleteTraceFile cmdResult has unexpected type = {cmdResult.GetType()}: '{cmdResult}'. CMD IS [{shellCommand}]");
							}
						}

						LogForDebug($"[Info]: DeleteTraceFile SUCCESS [{shellCommand}] finished on SQL Server. Success = {(xp_cmd_result.HasValue && xp_cmd_result.Value == 0 ? "SUCCESS" : $"Unknown {xp_cmd_result}")}");

                        // Console.WriteLine($"xp_cmdshell: TRACE FILE '{traceFile}.trc' successfully deleted");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning! xp_cmdshell failed to delete trace file '{traceFile}.trc. Please grant permission to the '{config.AppName}' app or switch off cleanup via xp_cmdshell.'{Environment.NewLine}{ex}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(config.SharedSqlTracesDirectory))
            {
                string fullPath = Path.Combine(config.SqlTracesDirectory, Path.GetFileName(traceFile) + ".trc");
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }

        }

        [Conditional("DEBUG")]
        static void LogForDebug(string message)
        {
	        Console.WriteLine(message);
        }
    }
}
