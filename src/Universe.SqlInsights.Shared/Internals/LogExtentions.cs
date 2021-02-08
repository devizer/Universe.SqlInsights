using System.Diagnostics;

namespace Universe.SqlInsights.Shared.Internals
{
    public static class LogExtentions
    {

        public static string FormatMilliseconds(this Stopwatch stopwatch)
        {
            if (stopwatch == null)
                return "null";

            return (1000d * stopwatch.ElapsedTicks / Stopwatch.Frequency).ToString("f2");
        }

        public static double GetMilliseconds(this Stopwatch stopwatch)
        {
            if (stopwatch == null)
                return -1;

            return (1000d * stopwatch.ElapsedTicks / Stopwatch.Frequency);
        }

        public static double? GetOptionalMilliseconds(this Stopwatch stopwatch)
        {
            if (stopwatch == null || !stopwatch.IsRunning)
                return null;

            return (1000d * stopwatch.ElapsedTicks / Stopwatch.Frequency);
        }

        public static double? Add(this double? one, double? other)
        {
            if (!one.HasValue && !other.HasValue)
                return null;

            return one.GetValueOrDefault() + other.GetValueOrDefault();
        }

    }
}
