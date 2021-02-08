using System.Collections.Generic;

namespace Universe.SqlInsights.Shared
{
    public interface ISqlInsightsStorage
    {
        // To Inject (NET 3.5 +) 
        void AddAction(ActionDetailsWithCounters reqAction);
        IEnumerable<long> GetAliveSessions();

        // For universe.sqltrace.w3app
        IEnumerable<ActionSummaryCounters> GetActionsSummary(long idSession);
        string GetActionsSummaryTimestamp(long idSession);
        IEnumerable<ActionDetailsWithCounters> GetActionsByKeyPath(long idSession, SqlInsightsActionKeyPath keyPath);
        string GetKeyPathTimestampOfDetails(long idSession, SqlInsightsActionKeyPath keyPath);

        IEnumerable<SqlInsightsSession> GetSessions();
        long CreateSession(string caption, int? maxDurationMinutes);
        void DeleteSession(long idSession);
        void RenameSession(long idSession, string caption);
        void FinishSession(long idSession);

    }
}
