using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.W3Api.Controllers
{
    [ApiController]
    [Route("api/" + ApiVer.V1 + "/SqlInsights/Sessions/[action]")]
    public partial class SessionsController : ControllerBase
    {

        private ILogger<SessionsController> _Logger;
        private readonly ISqlInsightsStorage _Storage;

        public SessionsController(ILogger<SessionsController> logger, ISqlInsightsStorage storage)
        {
            _Logger = logger;
            _Storage = storage;
        }

        [HttpPost]
        public async Task<ActionResult<IEnumerable<SqlInsightsSession>>> Sessions()
        {
            return (await _Storage.GetSessions()).ToJsonResult();
        }

        public class CreateSessionParameters
        {
            public string Caption { get; set; }
            public int? MaxDurationMinutes { get; set; }
        }

        public class ResumeSessionParameters
        {
            public long IdSession { get; set; }
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

            var idSession = await _Storage.CreateSession(args.Caption, args.MaxDurationMinutes);
            return idSession.ToJsonResult();
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<ActionResult> DeleteSession(IdSessionParameters args)
        {
            await _Storage.FinishSession(args.IdSession);

            Exception error = null;
            // as foreground to prevent abort on shutdown
            var thread = new Thread(() =>
            {
                // async void needs try/catch
                try
                {
                    // await _Storage.DeleteSession(args.IdSession);
                    _Storage.DeleteSession(args.IdSession).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    error = ex;
                    _Logger.LogError($"Delete Session {args.IdSession} failed. {ex.GetExceptionDigest()}");
                }
            }) { IsBackground = false };
            thread.Start();

            bool isCompleted = thread.Join(TimeSpan.FromSeconds(10));
            if (error != null)
                throw new Exception($"Deleting session {args.IdSession} failed", error);

            if (isCompleted)
                return "Completed".ToJsonResult(httpStatusCode: 200);
            else
                return "Accepted".ToJsonResult(httpStatusCode: 202);
        }

        [HttpPost]
        public async Task<ActionResult> FinishSession(IdSessionParameters args)
        {
            await _Storage.FinishSession(args.IdSession);
            return "OK".ToJsonResult();
        }

        [HttpPost]
        public async Task<ActionResult> ResumeSession(ResumeSessionParameters args)
        {
            await _Storage.ResumeSession(args.IdSession, args.MaxDurationMinutes);
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