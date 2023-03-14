using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
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
            
            // Console.WriteLine($"GetListenOnUrlsFromAppSettings(): {GetListenOnUrlsFromAppSettings()}");

            AssemblyVisualizer.Subscribe();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(configure =>
                {
                    configure.ServiceName = "SqlInsightsDashboard";
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

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
            var appLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(appLocation)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                    .Build();

                var listenOnUrl = configuration.GetValue<string>("ListenOnUrls");
                return !string.IsNullOrEmpty(listenOnUrl) ? listenOnUrl : null;
            }
            catch
            {
                return null;
            }
        }
    }
}