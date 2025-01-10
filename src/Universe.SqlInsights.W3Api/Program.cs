using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;
using Serilog.Events;
using Universe.SqlInsights.W3Api.Helpers;

namespace Universe.SqlInsights.W3Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if ("--version".Equals(args.FirstOrDefault(), StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(typeof(Program).Assembly.GetName().Version);
                return;
            }
            
            Console.WriteLine($"Starting SqlInsights Dashboard Web Api. Version: {typeof(Program).Assembly.GetName().Version}. Runtime Version: {RuntimeInformation.FrameworkDescription}.");

            var listenOnUrls = GetListenOnUrls();
            if (!string.IsNullOrEmpty(listenOnUrls))
            {
                Console.WriteLine($"Forced ASPNETCORE_URLS: '{listenOnUrls}'");
                Environment.SetEnvironmentVariable("ASPNETCORE_URLS", listenOnUrls);
            }

            // Fix Dapper Memory Leak
            SqlMapper.ConnectionStringComparer = SqlConnectionStringComparer.Instance;
            
            // Console.WriteLine($"GetListenOnUrlsFromAppSettings(): {GetListenOnUrlsFromAppSettings()}");

            AssemblyVisualizer.Subscribe();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder builder = Host.CreateDefaultBuilder(args);
            if (IsLogFilesEnabled)
            {
                builder
                    .UseSerilog((context, provider, config) =>
                    {
                        var outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] \"{SourceContext}\"{NewLine}{Message}{NewLine}{Exception}";

                        var logFolderFullName = GetLogFileFolder();
                        CreateDirectoryIfNotExists(logFolderFullName);

                        LogsGarbageCollector logsGarbageCollector = new LogsGarbageCollector(logFolderFullName);
                        logsGarbageCollector.Setup();

                        var logFileFullName = GetLogFileFullName();
                        Console.WriteLine($"[Startup Configuration] Local Log File Name: '{logFileFullName}'");
                        config.WriteTo.File(
                            logFileFullName,
                            outputTemplate: outputTemplate,
                            fileSizeLimitBytes: 128 * 1024 * 1024,
                            // fileSizeLimitBytes: 128 * 1024,
                            rollOnFileSizeLimit: true,
                            retainedFileCountLimit: 5,
                            flushToDiskInterval: TimeSpan.FromSeconds(1),
                            encoding: new UTF8Encoding(true),
                            shared: true,
                            buffered: false
                            // rollingInterval: RollingInterval.Hour
                            // retainedFileTimeLimit: 
                        );
                        config.WriteTo.Console(LogEventLevel.Information, outputTemplate: outputTemplate);
                    });
            }
            else
            {
                Console.WriteLine($"[Startup Configuration] Additional Local File Logging is Disabled");
            }

            builder
                .UseWindowsService(configure => { configure.ServiceName = "SqlInsightsDashboard"; })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

            return builder;
        }

        private static void CreateDirectoryIfNotExists(string dirName)
        {
            try
            {
                Directory.CreateDirectory(dirName);
            }
            catch
            {
            }
        }

        static string GetListenOnUrls()
        {
            if (WindowsServiceHelpers.IsWindowsService())
            {
                var sqlInsightsUrls = Environment.GetEnvironmentVariable("SQLINSIGHTS_DASHBOARD_URLS");
                if (!string.IsNullOrEmpty(sqlInsightsUrls)) return sqlInsightsUrls;
                var listenOnUrlFromAppSettings = GetListenOnUrlsFromAppSettings();
                if (!string.IsNullOrEmpty(listenOnUrlFromAppSettings)) return listenOnUrlFromAppSettings;
            }

            return null;
        }

        private static string GetListenOnUrlsFromAppSettings()
        {
            try
            {
                var configuration = ConfigurationRoot;

                var listenOnUrl = configuration.GetValue<string>("ListenOnUrls");
                return !string.IsNullOrEmpty(listenOnUrl) ? listenOnUrl : null;
            }
            catch
            {
                return null;
            }
        }

        // TODO: Lazy
        public static IConfigurationRoot ConfigurationRoot => _ConfigurationRoot.Value;

        private static Lazy<IConfigurationRoot> _ConfigurationRoot = new Lazy<IConfigurationRoot>(GetConfigurationRoot);
        private static IConfigurationRoot GetConfigurationRoot()
        {
            var appLocation = AppDirectory;
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(appLocation)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
            
            return configuration;
        }

        public static string AppDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);


        static string GetLogFileFolder()
        {
            var key = $"LocalLogsFolder:{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Unix")}";
            var ret = ConfigurationRoot.GetValue<string>(key);
            ret = string.IsNullOrEmpty(ret) ? null : Environment.ExpandEnvironmentVariables(ret);

            if (string.IsNullOrEmpty(ret))
            {
                // Only for development
                if (WindowsServiceHelpers.IsWindowsService())
                {
                    return Path.Combine(SystemDriveAccess.WindowsSystemDrive, "Temp", "SqlInsights Dashboard Logs");
                }

                // TODO: It is only for development
                return Path.Combine(AppDirectory, "Logs");
            }

            return ret;
        }

        static string GetLogFileFullName()
        {
            return Path.Combine(GetLogFileFolder(), DateTime.Now.ToString("yyyy-MM-dd HH꞉mm꞉ss") + ".log");
        }

        static bool IsLogFilesEnabled => ConfigurationRoot.GetBooleanValue("LocalLogsFolder:Enable");
    }

    class SqlConnectionStringComparer : IEqualityComparer<string>
    {
        public static IEqualityComparer<string> Instance = new SqlConnectionStringComparer();

        public bool Equals(string x, string y)
        {
            return true;
        }

        public int GetHashCode(string obj)
        {
            return 42;
        }
    }
}