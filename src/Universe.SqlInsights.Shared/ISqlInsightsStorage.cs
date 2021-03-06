﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universe.SqlInsights.Shared
{
    public interface ISqlInsightsStorage
    {
        // To Inject (NET 3.5 +) 
        void AddAction(ActionDetailsWithCounters reqAction);
        IEnumerable<long> GetAliveSessions();
        bool AnyAliveSession();

        // For universe.sqltrace.w3app
#if NETSTANDARD        
        Task<IEnumerable<ActionSummaryCounters>> GetActionsSummary(long idSession, string optionalApp, string optionalHost);
        Task<string> GetActionsSummaryTimestamp(long idSession, string optionalApp, string optionalHost);
        Task<IEnumerable<ActionDetailsWithCounters>> GetActionsByKeyPath(long idSession, SqlInsightsActionKeyPath keyPath, int lastN, string optionalApp, string optionalHost);
        Task<string> GetKeyPathTimestampOfDetails(long idSession, SqlInsightsActionKeyPath keyPath, string optionalApp, string optionalHost);

        Task<IEnumerable<SqlInsightsSession>> GetSessions();
        Task<long> CreateSession(string caption, int? maxDurationMinutes);
        Task DeleteSession(long idSession);
        Task RenameSession(long idSession, string caption);
        Task FinishSession(long idSession);

        Task<IEnumerable<LongAndString>> GetAppNames();
        Task<IEnumerable<LongAndString>> GetHostIds();
#endif
    }

    public class LongAndString
    {
        public long Id { get; set; }
        public string Value { get; set; }
    }
}
