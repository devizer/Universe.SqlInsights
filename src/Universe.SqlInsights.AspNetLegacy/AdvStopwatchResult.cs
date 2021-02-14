namespace Universe.SqlInsights.AspNetLegacy
{
    public class AdvStopwatchResult
    {
        public double Time { get; private set; }

        public double KernelUsage { get; private set; }
        public double UserUsage { get; private set; }

        public double CpuUsage
        {
            get { return KernelUsage + UserUsage; }
        }

        public AdvStopwatchResult(double time, double kernelUsage, double userUsage)
        {
            Time = time;
            KernelUsage = kernelUsage;
            UserUsage = userUsage;
        }

        public static AdvStopwatchResult Substruct(AdvStopwatchResult one, AdvStopwatchResult another)
        {
            return new AdvStopwatchResult(
                one.Time - another.Time,
                one.KernelUsage - another.KernelUsage,
                one.UserUsage - another.UserUsage);
        }

    }

    public static class StopwatchResultExtensions
    {
        static AdvStopwatchResult Zero = new AdvStopwatchResult(0,0,0);
        public static AdvStopwatchResult Add(this AdvStopwatchResult one, AdvStopwatchResult another)
        {
            if (one == null && another == null)
                return null;

            return new AdvStopwatchResult(
                (one ?? Zero).Time + (another ?? Zero).Time,
                (one ?? Zero).KernelUsage + (another ?? Zero).KernelUsage,
                (one ?? Zero).UserUsage + (another ?? Zero).UserUsage);
        }

        public static AdvStopwatchResult GetOptionalResult(this AdvStopwatch arg)
        {
            if (arg == null || !arg.IsRunning)
                return null;

            return arg.Result;
        }

        public static string ToHumanString(this AdvStopwatchResult swr)
        {
            if (swr == null) return "";
            return $"{{Duration: {swr.Time}, CPU: {swr.CpuUsage} = {swr.UserUsage} [user] + {swr.KernelUsage} [kernel]}}";
        }

    }
}