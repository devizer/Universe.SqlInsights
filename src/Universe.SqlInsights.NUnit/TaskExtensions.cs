﻿using System.Threading.Tasks;

namespace Universe.SqlInsights.NUnit
{
    public static class TaskExtensions
    {
        public static T GetSafeResult<T>(this Task<T> task)
        {
            return task.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
        }

        public static void SafeWait(this Task task)
        {
            task.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().GetResult();
        }
    }
}