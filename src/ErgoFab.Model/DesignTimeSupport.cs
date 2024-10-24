using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace ErgoFab.Model
{
    internal class DesignTimeSupport
    {
        public static string GetDesignTimeConnectionString()
        {
            var configuration = ConfigurationRoot;
            var ret = configuration.GetValue<string>("ConnectionStrings:DesignTime");
            Console.WriteLine($"Design-time Connection String: {ret}");
            return ret;
        }

        public static IConfigurationRoot ConfigurationRoot => _ConfigurationRoot.Value;

        private static Lazy<IConfigurationRoot> _ConfigurationRoot = new(GetConfigurationRoot);

        private static IConfigurationRoot GetConfigurationRoot()
        {
            Console.WriteLine($"Design-time settings: \"{Path.Combine(AppDirectory, "appsettings.Design.json")}\"");
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDirectory)
                .AddJsonFile("appsettings.Design.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            return configuration;
        }

        public static string AppDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}
