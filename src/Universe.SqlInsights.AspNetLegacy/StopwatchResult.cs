namespace Universe.SqlInsights.AspNetLegacy
{
    public class StopwatchResult
    {
        public double Time { get; private set; }

        public double KernelUsage { get; private set; }
        public double UserUsage { get; private set; }

        public double CpuUsage
        {
            get { return KernelUsage + UserUsage; }
        }

        public StopwatchResult(double time, double kernelUsage, double userUsage)
        {
            Time = time;
            KernelUsage = kernelUsage;
            UserUsage = userUsage;
        }

        public static StopwatchResult Substruct(StopwatchResult one, StopwatchResult another)
        {
            return new StopwatchResult(
                one.Time - another.Time,
                one.KernelUsage - another.KernelUsage,
                one.UserUsage - another.UserUsage);
        }

    }

    public static class StopwatchResultExtensions
    {
        static StopwatchResult Zero = new StopwatchResult(0,0,0);
        public static StopwatchResult Add(this StopwatchResult one, StopwatchResult another)
        {
            if (one == null && another == null)
                return null;

            return new StopwatchResult(
                (one ?? Zero).Time + (another ?? Zero).Time,
                (one ?? Zero).KernelUsage + (another ?? Zero).KernelUsage,
                (one ?? Zero).UserUsage + (another ?? Zero).UserUsage);
        }

        public static StopwatchResult GetOptionalResult(this AdvStopwatch arg)
        {
            if (arg == null || !arg.IsRunning)
                return null;

            return arg.Result;
        }

        public static string ToHumanString(this StopwatchResult swr)
        {
            if (swr == null) return "";
            return $"{{Duration: {swr.Time}, CPU: {swr.CpuUsage} = {swr.UserUsage} [user] + {swr.KernelUsage} [kernel]}}";
        }

    }
}