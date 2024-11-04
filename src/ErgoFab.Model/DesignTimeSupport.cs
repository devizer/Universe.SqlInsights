using System.ComponentModel.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace ErgoFab.Model
{
    internal class DesignTimeSupport
    {
        public static string GetDesignTimeConnectionString()
        {
            string ret;
            var key = "ERGOFAB_DESIGN_TIME_CONNECTION_STRING";
            var raw = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(raw))
                ret = raw;
            else if (File.Exists(key)) 
                ret = File.ReadAllText(key).Trim(new char[] { '\r', '\n', '\t', ' ' });
            else
                throw new InvalidOperationException($"Either file or environment variable \"{key}\" is required for design time");

            return ret;
        }

        public static string AppDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}
