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
        public async Task<ActionResult<string>> ActionsTimestamp(ActionsParameters args)
        {
            var keyPath = ParseActionKeyPath(args.Path);
            string timestamp = await _Storage.GetKeyPathTimestampOfDetails(args.IdSession, keyPath, args.AppName, args.HostId);
            return ToJsonResult(timestamp);
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<ActionDetailsWithCounters>>> ActionsByKey(ActionsParameters args)
        {
            SqlInsightsActionKeyPath keyPath = ParseActionKeyPath(args.Path);
            IEnumerable<ActionDetailsWithCounters> ret = await _Storage.GetActionsByKeyPath(args.IdSession, keyPath, 100, args.AppName, args.HostId);
            return ToJsonResult(ret);
        }

        public class ActionsParameters
        {
            public long IdSession { get; set; }
            public string[] Path { get; set; }
            public string AppName { get; set; }
            public string HostId { get; set; }
        }
        
        public class ActionsSummaryParameters
        {
            public long IdSession { get; set; }
            public string AppName { get; set; }
            public string HostId { get; set; }
        }
        

        [HttpPost]
        public async Task<ActionResult<IEnumerable<ActionSummaryCounters>>> Summary(ActionsSummaryParameters args)
        {
            IEnumerable<ActionSummaryCounters> ret = await _Storage.GetActionsSummary(args.IdSession, args.AppName, args.HostId);
            return ToJsonResult(ret);
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<ActionSummaryCounters>>> SummaryTimeStamp(ActionsSummaryParameters args)
        {
            string ret = await _Storage.GetActionsSummaryTimestamp(args.IdSession, args.AppName, args.HostId);
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