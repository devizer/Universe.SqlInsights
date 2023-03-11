using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Universe.SqlInsights.W3Api.Helpers;

namespace Universe.SqlInsights.W3Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AssemblyVisualizer.Subscribe();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}