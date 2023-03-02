using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage
{
    partial class SqlServerSqlInsightsStorage 
    {
        public IEnumerable<long> GetAliveSessions()
        {
            const string sql = @"
DECLARE @UtcNow datetime; 
Set @UtcNow = GetUtcDate();
SELECT 
    IdSession
From 
    [SqlInsightsSession]
Where
    IsFinished = (0) 
    And (MaxDurationMinutes Is Null Or DateAdd(minute,MaxDurationMinutes,StartedAt) >= @UtcNow)";
            
            using (var con = GetConnection())
            {
                var query = con.Query<long>(sql, null);
                return query.ToList();
            }
        }

        public bool AnyAliveSession()
        {
            const string sql = @"
DECLARE @UtcNow datetime; 
Set @UtcNow = GetUtcDate(); 
If Exists
(
    SELECT 
        IdSession 
    From 
        [SqlInsightsSession] 
    Where 
        IsFinished = (0) 
        And (MaxDurationMinutes Is Null Or DateAdd(minute,MaxDurationMinutes,StartedAt) >= @UtcNow)
) 
Select 1 [Any]";
            using (var con = GetConnection())
            {
                int? queryResult = con.ExecuteScalar<int?>(sql, null, null, null, CommandType.Text);
                return queryResult.HasValue && queryResult.Value != 0;
            }
        }


#if NETSTANDARD
        
        public async Task<IEnumerable<SqlInsightsSession>> GetSessions()
        {
            const string sql = "Select IdSession, StartedAt, EndedAt, IsFinished, Caption, MaxDurationMinutes From SqlInsightsSession";
            using (var con = GetConnection())
            {
                var query = await con.QueryAsync<SqlInsightsSession>(sql, null);
                return query.ToList();
            }
        }

        public async Task<long> CreateSession(string caption, int? maxDurationMinutes)
        {
            const string sql = @"
Insert 
    SqlInsightsSession(StartedAt, IsFinished, Caption, MaxDurationMinutes)
    OUTPUT INSERTED.IdSession As IdSession -- May return IdSession, but fail     
Values(
    GetUtcDate(),
    (0),
    @Caption,
    @MaxDurationMinutes
);   
-- Select SCOPE_IDENTITY() As IdSession;  
";

            using (var con = GetConnection())
            {
                var ret = await con.ExecuteScalarAsync<long>(sql, new {Caption = caption, MaxDurationMinutes = maxDurationMinutes});
                return ret;
            }
        }

        public async Task DeleteSession(long idSession)
        {
            string[] sqlList = new[]
            {
                "Delete From SqlInsightsAction Where IdSession = @IdSession;",
                "Delete From SqlInsightsKeyPathSummary Where IdSession = @IdSession;",
                "Delete From SqlInsightsSession Where IdSession = @IdSession;"
            };
        
            using (var con = GetConnection())
            {
                foreach (var sql in sqlList)
                {
                    await con.ExecuteAsync(sql, new {IdSession = idSession});
                }
            }
        }

        public async Task RenameSession(long idSession, string caption)
        {
            const string sql = @"Update SqlInsightsSession Set Caption = @Caption Where IdSession = @IdSession;"; 
            using (var con = GetConnection())
            {
                await con.ExecuteAsync(sql, new {IdSession = idSession, Caption = caption});
            }
        }

        public async Task FinishSession(long idSession)
        {
            // if (idSession == 0) return; 
            const string sql = @"Update SqlInsightsSession Set IsFinished = (1), EndedAt = GETUTCDATE() Where IdSession = @IdSession;"; 
            using (var con = GetConnection())
            {
                await con.ExecuteAsync(sql, new {IdSession = idSession});
            }
        }

#endif
    }
}