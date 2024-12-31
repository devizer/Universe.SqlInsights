using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.W3Api.Controllers
{
    [ApiController]
    [Route("api/" + ApiVer.V1 + "/SqlInsights/[action]")]
    public partial class SqlInsightsController : ControllerBase
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
            string timestamp = await _Storage.GetKeyPathTimestampOfDetails(args.IdSession, keyPath, args.AppsFilter, args.HostsFilter);
            return timestamp.ToJsonResult();
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<ActionDetailsWithCounters>>> ActionsByKey(ActionsParameters args)
        {
            SqlInsightsActionKeyPath keyPath = ParseActionKeyPath(args.Path);
            var topN = Math.Max(1,Math.Min(10000, args.TopN));
            if (args.TopN == 0) topN = 100;
            IEnumerable<ActionDetailsWithCounters> ret = await _Storage.GetActionsByKeyPath(args.IdSession, keyPath, topN, args.AppsFilter, args.HostsFilter, args.IsOK);
            return ret.ToJsonResult();
        }

        public class ActionsParameters
        {
            public long IdSession { get; set; }
            public string[] Path { get; set; }
            // public string AppName { get; set; }
            // public string HostId { get; set; }
            public string[] AppsFilter { get; set; }
            public string[] HostsFilter { get; set; }

            // null - any, true - only success, false - only fail
            public bool? IsOK { get; set; } = null;
            public int TopN { get; set; } = 100;
        }

        public enum ParameterErrorsType
        {
            // All the Actions
            Any,
            // Only Failed
            OnlyErrors,
            // Only Success
            OnlySuccess,
        }

        public class ActionsSummaryParameters
        {
            public long IdSession { get; set; }
            // public string AppName { get; set; }
            // public string HostId { get; set; }
            public string[] AppsFilter { get; set; }
            public string[] HostsFilter { get; set; }
        }

        

        [HttpPost]
        public async Task<ActionResult<IEnumerable<ActionSummaryCounters>>> Summary(ActionsSummaryParameters args)
        {
            IEnumerable<ActionSummaryCounters> ret = await _Storage.GetActionsSummary(args.IdSession, args.AppsFilter, args.HostsFilter);
            return ret.ToJsonResult();
        }

        [HttpPost]
        public async Task<ActionResult<string>> SummaryTimeStamp(ActionsSummaryParameters args)
        {
            string ret = await _Storage.GetActionsSummaryTimestamp(args.IdSession, args.AppsFilter, args.HostsFilter);
            // What the heck
            return ret.ToJsonResult();
        }

        [HttpPost]
        public async Task<ActionResult<FiltersResult>> PopulateFilters()
        {
            IEnumerable<LongIdAndString> appNames = await _Storage.GetAppNames();
            IEnumerable<LongIdAndString> hostIdList = await _Storage.GetHostIds();
            FiltersResult ret = new FiltersResult()
            {
                ApplicationList = appNames?.Select(x => new AppFilter() { App = x.Value }),
                HostIdList = hostIdList?.Select(x => new HostIdFilter() { HostId = x.Value })
            };
            return ret.ToJsonResult();
        }

        public class FiltersResult
        {
            public IEnumerable<AppFilter> ApplicationList { get; set; }
            public IEnumerable<HostIdFilter> HostIdList { get; set; }
        }

        public class AppFilter
        {
            public string App { get; set; }
        }
        public class HostIdFilter
        {
            public string HostId { get; set; }
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
   }
}