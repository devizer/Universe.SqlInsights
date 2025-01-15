using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Linq;
using Newtonsoft.Json;
using Universe.SqlInsights.Shared;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Universe.SqlInsights.W3Api.Helpers
{
    public class JsonExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerFactory _loggerFactory;
        private readonly JsonExceptionHandlerMiddlewareCustomContext appInfo;

        public JsonExceptionHandlerMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, JsonExceptionHandlerMiddlewareCustomContext appInfo)
        {
            _next = next;
            _loggerFactory = loggerFactory;
            this.appInfo = appInfo;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                var dataRouting = context?.GetRouteData()?.Values?.Select(x => new KeyValuePair<string, object>(x.Key, x.Value));
                var contextData = appInfo.Concat(dataRouting);
                
                // Duplicate Keys?
                Dictionary<string, object> contextDictionary = new Dictionary<string, object>();
                foreach (var pair in contextData) contextDictionary[pair.Key] = pair.Value?.ToString();

                var error = ex;
                var url = $"{context?.Request?.Method} {context?.Request?.GetDisplayUrl()}";
                var errorJson = new
                {
                    Type = error?.GetType().Name ?? "Error 500",
                    Title = error?.GetExceptionDigest() ?? "Error 500",
                    Url = url,
                    Context = contextDictionary,
                    StackTrace = error?.ToString(),
                };

                var logger = _loggerFactory.CreateLogger("ASP.NET Core Pipeline Error");
                logger.LogError(ex, "Http Request {url} failed. {error}", url, ex.GetExceptionDigest());
                await context.Response.WriteAsync(JsonConvert.SerializeObject(errorJson));
            }
        }
    }

    public class JsonExceptionHandlerMiddlewareCustomContext : Collection<KeyValuePair<string, object>>
    {
        public void Add(string key, object value) => Add(new KeyValuePair<string, object>(key, value));
    }
}
