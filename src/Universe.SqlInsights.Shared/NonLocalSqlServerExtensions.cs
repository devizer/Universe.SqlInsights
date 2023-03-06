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
                    string shellCommand = $"del \"{traceFile}.trc\"";
                    string sql = "DECLARE @result int; Exec @result = xp_cmdshell @command_string; Select @Result;";
                    try
                    {
                        con.Open();
                        // TODO: ONLY OF WINDOWS !!!!!!!!!!!!!!!!!!!!!!!
                        using (SqlCommand cmd = new SqlCommand(sql, con))
                        {
                            cmd.Parameters.Add("@command_string", SqlDbType.NVarChar, 4000).Value = shellCommand;
                            cmd.ExecuteNonQuery();
                        }

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
