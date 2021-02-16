using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universe.SqlInsights.Shared
{
    public interface ISqlInsightsStorage
    {
        // To Inject (NET 3.5 +) 
        void AddAction(ActionDetailsWithCounters reqAction);
        IEnumerable<long> GetAliveSessions();

        // For universe.sqltrace.w3app
#if NETSTANDARD        
        Task<IEnumerable<ActionSummaryCounters>> GetActionsSummary(long idSession);
        Task<string> GetActionsSummaryTimestamp(long idSession);
        Task<IEnumerable<ActionDetailsWithCounters>> GetActionsByKeyPath(long idSession, SqlInsightsActionKeyPath keyPath, int lastN);
        Task<string> GetKeyPathTimestampOfDetails(long idSession, SqlInsightsActionKeyPath keyPath);

        Task<IEnumerable<SqlInsightsSession>> GetSessions();
        Task<long> CreateSession(string caption, int? maxDurationMinutes);
        Task DeleteSession(long idSession);
        Task RenameSession(long idSession, string caption);
        Task FinishSession(long idSession);
#endif
    }
}
