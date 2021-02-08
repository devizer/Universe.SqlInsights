using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.W3Api.Controllers
{
    [ApiController]
    [Route("api/" + ApiVer.V1 + "/SqlInsights/[action]")]
    public class SqlInsightsController : ControllerBase
    {
        private readonly ISqlInsightsStorage _Storage;
        private const long IdSessionStub = 0;

        public SqlInsightsController(ISqlInsightsStorage storage)
        {
            _Storage = storage;
        }

        [HttpPost]
        public async Task<ActionResult<string>> ActionsTimestamp(KeyPathModel key)
        {
            var keyPath = ParseActionKeyPath(key.Path);
            string timestamp = _Storage.GetKeyPathTimestampOfDetails(IdSessionStub, keyPath);
            return ToJsonResult(timestamp);
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<ActionDetailsWithCounters>>> ActionsByKey(KeyPathModel key)
        {
            SqlInsightsActionKeyPath keyPath = ParseActionKeyPath(key.Path);
            IEnumerable<ActionDetailsWithCounters> ret = _Storage.GetActionsByKeyPath(IdSessionStub, keyPath);
            return ToJsonResult(ret);
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<ActionSummaryCounters>>> Summary()
        {
            IEnumerable<ActionSummaryCounters> ret = _Storage.GetActionsSummary(IdSessionStub);
            return ToJsonResult(ret);
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<ActionSummaryCounters>>> SummaryTimeStamp()
        {
            string ret = _Storage.GetActionsSummaryTimestamp(IdSessionStub);
            return ToJsonResult(ret);
        }

        public class KeyPathModel
        {
            public string[] Path { get; set; }
        }

        static SqlInsightsActionKeyPath ParseActionKeyPath(string groupId)
        {
            if (groupId == null) throw new ArgumentNullException(nameof(groupId), "keyPath is required");
            var keyPath = groupId.Split('→').Select(x => x.Trim()).ToArray();
            return new SqlInsightsActionKeyPath(keyPath);
        }
        static SqlInsightsActionKeyPath ParseActionKeyPath(string[] path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path), "keyPath is required");
            return new SqlInsightsActionKeyPath(path);
        }
        
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        private static ActionResult ToJsonResult(object obj)
        {
            return new ContentResult()
            {
                Content = obj == null ? "null" : JsonConvert.SerializeObject(obj, SerializerSettings),
                ContentType = "application/json",
            };
        }
   }
}