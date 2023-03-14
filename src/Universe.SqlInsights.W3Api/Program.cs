using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
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

            if (WindowsServiceHelpers.IsWindowsService())
            {
                var sqlInsightsUrls = Environment.GetEnvironmentVariable("SQLINSIGHTS_DASHBOARD_URLS");
                if (!string.IsNullOrEmpty(sqlInsightsUrls))
                    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", sqlInsightsUrls);
            }

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
    }
}