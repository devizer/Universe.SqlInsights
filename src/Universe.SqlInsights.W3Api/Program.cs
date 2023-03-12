using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
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
            
            Console.WriteLine($"Runtime Version: {RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"SqlInsights Dashboard Web Api Version: {typeof(Program).Assembly.GetName().Version}");

            AssemblyVisualizer.Subscribe();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}