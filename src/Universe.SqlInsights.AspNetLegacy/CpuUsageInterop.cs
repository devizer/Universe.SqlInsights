using System;
using System.Runtime.InteropServices;

namespace Universe.SqlInsights.AspNetLegacy
{
    public class CpuUsageInterop
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetThreadTimes(IntPtr hThread, out long lpCreationTime,
            out long lpExitTime, out long lpKernelTime, out long lpUserTime);

        public static bool GetThreadTimes(out double kernelTime, out double userTime)
        {
            long ignored;
            long kernel;
            long user;
            if (GetThreadTimes(GetCurrentThread(), out ignored, out ignored, out kernel, out user))
            {
                kernelTime = kernel / 10000d;
                userTime = user / 10000d;
                return true;
            }
            else
            {
                kernelTime = 0;
                userTime = 0;
                return false;
            }

        }
    }
}