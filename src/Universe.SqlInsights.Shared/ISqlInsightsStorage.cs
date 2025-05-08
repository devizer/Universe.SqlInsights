using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universe.SqlInsights.Shared
{
    public interface ITraceableStorage
    {
        string ConnectionString { get; set; }
    }
    
    public interface ISqlInsightsStorage
    {
        // To Inject (NET 3.5 +) 
        // TODO: ASYNC
        void AddAction(ActionDetailsWithCounters reqAction);
        
        // Used by AddAction only
        IEnumerable<long> GetAliveSessions();
        bool AnyAliveSession();

        // For universe.sqltrace.w3app
#if NETSTANDARD || NET461
        Task<IEnumerable<ActionSummaryCounters>> GetActionsSummary(long idSession, IEnumerable<string> optionalApps, IEnumerable<string> optionalHosts);
        Task<string> GetActionsSummaryTimestamp(long idSession, IEnumerable<string> optionalApps, IEnumerable<string> optionalHosts);
        // keyPath != null for Dashboard, null for Export (1 sec for 20000 actions)
        Task<IEnumerable<ActionDetailsWithCounters>> GetActionsByKeyPath(long idSession, SqlInsightsActionKeyPath keyPath, int lastN, IEnumerable<string> optionalApps, IEnumerable<string> optionalHosts, bool? isOk);
        Task<string> GetKeyPathTimestampOfDetails(long idSession, SqlInsightsActionKeyPath keyPath, IEnumerable<string> optionalApps, IEnumerable<string> optionalHosts);

        Task<IEnumerable<SqlInsightsSession>> GetSessions();
        Task<long> CreateSession(string caption, int? maxDurationMinutes);
        Task DeleteSession(long idSession);
        Task RenameSession(long idSession, string newCaption);
        Task FinishSession(long idSession);
        Task ResumeSession(long idSession, int? maxDurationMinutes);


        Task<IEnumerable<LongIdAndString>> GetAppNames();
        Task<IEnumerable<LongIdAndString>> GetHostIds();
#endif
    }

    public class LongIdAndString
    {
        public long Id { get; set; }
        public string Value { get; set; }
    }
}
