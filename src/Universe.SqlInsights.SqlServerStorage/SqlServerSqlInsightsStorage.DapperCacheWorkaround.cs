using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Dapper;

namespace Universe.SqlInsights.SqlServerStorage
{
    // Dapper Uses Connection string is a part of cache key
    public partial class SqlServerSqlInsightsStorage
    {
        private static long CommandCounter = 0;

        static void FlushDapperMemoryLeaks()
        {

            // TODO: Only if Self Traced
            long commandCounter = Interlocked.Increment(ref CommandCounter);
            if (commandCounter % 1000 == 0)
            {
                SqlMapper.PurgeQueryCache();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
    }
}
