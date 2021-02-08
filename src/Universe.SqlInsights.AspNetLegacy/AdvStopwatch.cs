using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Universe.SqlInsights.Shared.Internals;

namespace Universe.SqlInsights.AspNetLegacy
{
    public class AdvStopwatch
    {
        private double _kernel, _user;
        private Stopwatch _stopWatch;
        public bool IsRunning { get; private set; }

        public void Start()
        {
            _stopWatch = Stopwatch.StartNew();
            IsRunning = true;
            if (!CpuUsageInterop.GetThreadTimes(out _kernel, out _user))
            {
                // wha?
                int error = Marshal.GetLastWin32Error();
                string msg = new Win32Exception(error).Message;
                Debug.WriteLine("Kernel32 doesn't work properly. " + msg);
                IsRunning = false;
            }
        }

        public StopwatchResult Result
        {
            get
            {
                double kernel;
                double user;
                CpuUsageInterop.GetThreadTimes(out kernel, out user);
                return new StopwatchResult(
                    _stopWatch.GetMilliseconds(),
                    kernel - _kernel,
                    user - _user);
            }
        }

        public static AdvStopwatch StartNew()
        {
            var ret = new AdvStopwatch();
            ret.Start();
            return ret;
        }
    }
}