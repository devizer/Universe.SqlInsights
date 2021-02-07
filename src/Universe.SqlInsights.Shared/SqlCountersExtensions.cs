using System;
using Universe.SqlTrace;

namespace Universe.SqlInsights.Shared
{
    public static class SqlCountersExtensions
    {
        private static readonly SqlCounters Zero = new SqlCounters() {Requests = 0};
        public static SqlCounters Add(this SqlCounters one, SqlCounters another)
        {
            if (one == null && another == null)
                return null;

            one ??= Zero;
            another ??= Zero;
            return new SqlCounters()
            {
                Duration = one.Duration + another.Duration,
                CPU = one.CPU + another.CPU,
                Writes = one.Writes + another.Writes,
                Reads = one.Reads + another.Reads,
                Requests = one.Requests + another.Requests,
            };
        }

        public static SqlCounters Substract(this SqlCounters one, SqlCounters another)
        {
            if (one == null)
                throw new ArgumentNullException(nameof(one));

            if (another == null)
                throw new ArgumentNullException(nameof(another));

            return new SqlCounters()
            {
                CPU = one.CPU - another.CPU,
                Writes = one.Writes - another.Writes,
                Duration = one.Duration - another.Duration,
                Reads = one.Reads - another.Reads,
                Requests = one.Requests - another.Requests,
            };
        }
    }
}