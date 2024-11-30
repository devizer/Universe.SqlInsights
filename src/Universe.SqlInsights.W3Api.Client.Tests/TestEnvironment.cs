using System;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
    public class TestEnvironment
    {
        public static string W3ApiBaseUri => GetW3ApiBaseUrl();

        private static string GetW3ApiBaseUrl()
        {
            var ret = Environment.GetEnvironmentVariable("S5_W3API_BASE_URL");
            if (string.IsNullOrEmpty(ret))
                ret = "http://localhost:50420";

            if (!ret.EndsWith("/")) ret += "/";
            return ret;
        }
    }
}
