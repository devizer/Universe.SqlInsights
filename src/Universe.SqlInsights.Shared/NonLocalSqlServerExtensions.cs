using System;
using System.Data;
using System.Data.SqlClient;
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
                    // TODO: Replace Hack by explicit tracefile arg
                    string shellCommand = $"del \"{traceFile}.trc\"";
                    string sql = @"
Declare @host_platform nvarchar(4000) = null;
If ((@@MICROSOFTVERSION / 16777216) <= 13) Set @host_platform = 'Windows' -- Version up to 2016
If ((@@MICROSOFTVERSION / 16777216) >= 14) -- Version 2017+
  And Exists (Select 1 From sys.all_objects Where name = 'dm_os_host_info' and type = 'V' and is_ms_shipped = 1)
Select @host_platform = host_platform from sys.dm_os_host_info;

If (@host_platform = 'Windows')
Begin
  DECLARE @result int;
  @command_string = REPLACE(@command_string, '/', '\');
  Exec @result = xp_cmdshell @command_string; 
  Select @Result;
End";
                    try
                    {
                        con.Open();
                        // TODO: ONLY ON WINDOWS !!!!!!!!!!!!!!!!!!!!!!!
                        using (SqlCommand cmd = new SqlCommand(sql, con))
                        {
                            cmd.Parameters.Add("@command_string", SqlDbType.NVarChar, 4000).Value = shellCommand;
                            cmd.ExecuteNonQuery();
                        }
                        
                        Console.WriteLine($"SUCCESS: [{shellCommand}] successfully completed");

                        // Console.WriteLine($"xp_cmdshell: TRACE FILE '{traceFile}.trc' successfully deleted");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"xp_cmdshell failed to delete trace file '{traceFile}.trc. Please grant permission to the '{config.AppName}' app or switch off cleanup via xp_cmdshell.'{Environment.NewLine}{ex}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(config.SharedSqlTracesDirectory))
            {
                string fullPath = Path.Combine(config.SqlTracesDirectory, Path.GetFileName(traceFile) + ".trc");
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }

        }
    }
}
