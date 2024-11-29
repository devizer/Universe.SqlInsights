using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Slices.Dashboard;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
    class S5ApiClientFactory
    {
        private static HttpClient _HttpClient = new HttpClient();
        public static S5ApiClient Create()
        {
            return new S5ApiClient(TestEnvironment.W3ApiBaseUri, _HttpClient);
        }
    }

    public static class DebugExtensions
    {
        public static string AsJson(this object arg)
        {
            JsonSerializer ser = new JsonSerializer()
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };

            StringBuilder json = new StringBuilder();
            StringWriter jwr = new StringWriter(json);
            ser.Serialize(jwr, arg);
            jwr.Flush();

            return json.ToString();
        }

    }
}