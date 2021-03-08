using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.W3Api.Controllers
{
    [ApiController]
    [Route("api/" + ApiVer.V1 + "/SqlInsights/Sessions/[action]")]
    public partial class SessionsController : ControllerBase
    {
        
        private readonly ISqlInsightsStorage _Storage;

        public SessionsController(ISqlInsightsStorage storage)
        {
            _Storage = storage;
        }


        [HttpPost]
        public async Task<ActionResult<IEnumerable<SqlInsightsSession>>> Index()
        {
            return (await _Storage.GetSessions()).ToJsonResult();
        }

        public class CreateSessionParameters
        {
            public string Caption { get; set; }
            public int? MaxDurationMinutes { get; set; }
        }

        public class IdSessionParameters
        {
            public long IdSession { get; set; }
        }

        public class RenameSessionParameters
        {
            public long IdSession { get; set; }
            public string Caption { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult<long>> CreateSession(CreateSessionParameters args)
        {
            if (string.IsNullOrEmpty(args?.Caption))
                throw new ArgumentException("Caption is expected", nameof(args.Caption));

            return (await _Storage.CreateSession(args.Caption, args.MaxDurationMinutes)).ToJsonResult();
        }

        [HttpPost]
        public async Task<ActionResult> DeleteSession(IdSessionParameters args)
        {
            await _Storage.DeleteSession(args.IdSession);
            return "OK".ToJsonResult();
        }

        [HttpPost]
        public async Task<ActionResult> FinishSession(IdSessionParameters args)
        {
            await _Storage.FinishSession(args.IdSession);
            return "OK".ToJsonResult();
        }

        [HttpPost]
        public async Task<ActionResult> RenameSession(RenameSessionParameters args)
        {
            if (string.IsNullOrEmpty(args?.Caption))
                throw new ArgumentException("Caption is expected", nameof(args.Caption));

            await _Storage.RenameSession(args.IdSession, args.Caption);
            return "OK".ToJsonResult();
        }
   }
}