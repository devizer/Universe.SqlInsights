using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Universe.SqlInsights.W3Api.Client.Tests
{
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