using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Universe.SqlInsights.W3Api.Controllers
{
    public static class NewtonsoftExtensions
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MaxDepth = 128,
        };

        public static ActionResult ToJsonResult(this object obj, int httpStatusCode = 200)
        {
            return new ContentResult()
            {
                Content = obj == null ? "null" : JsonConvert.SerializeObject(obj, SerializerSettings),
                ContentType = "application/json",
                StatusCode = httpStatusCode
            };
        }
    }
}