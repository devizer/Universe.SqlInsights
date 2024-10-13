using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.W3Api
{
    public class LogsGarbageLimits
    {
        public static readonly LogsGarbageLimits Default = new LogsGarbageLimits();

        // if number of log files exceed 1000, then old files are recycled.
        public readonly int FilesLimit = 12;

        // logs older then 240 hours are recycled.
        public readonly int HoursLimit = 240;

        // if total size exceeds 64Mb, then old logs are recycled
        public readonly int TotalSizeLimit = 512;

        // if single log file exceed 10Mb, then new log file is created
        // Never used.
        public readonly int SingleFileLimit = 10;
    }

    public class LogsGarbageCollector
    {
        private readonly string _logExtension = ".log";
        private readonly LogsGarbageLimits _limits = new LogsGarbageLimits();
        private TimeSpan _interval = TimeSpan.FromHours(1);
        private readonly string _folderName;

        public LogsGarbageCollector(string folderName)
        {
            _folderName = folderName;
        }

        public void Setup()
        {
            Timer t = new Timer(state => CleanUp(), null, TimeSpan.FromMilliseconds(1), _interval);
        }

        private static volatile bool IsCleanUP = false;

        internal void CleanUp()
        {

            if (IsCleanUP)
                return;

            IsCleanUP = true;

            try
            {
                string folder = _folderName;
                FileInfo[] fla = new DirectoryInfo(folder).GetFiles("*" + _logExtension);
                DateTime[] dates = new DateTime[fla.Length];
                for (int i = 0; i < dates.Length; i++)
                    dates[i] = fla[i].LastWriteTime;

                Array.Sort(dates, fla);
                Array.Reverse(fla);
                List<string> fullNameList = new List<string>();
                int n = 0;
                long sumSize = 0;
                DateTime lastDate = DateTime.Now - TimeSpan.FromHours(_limits.HoursLimit);
                bool isObsolete = false;
                foreach (FileInfo fi in fla)
                {
                    if (isObsolete)
                        fullNameList.Add(fi.FullName);

                    else
                    {
                        n++;
                        if (n > _limits.FilesLimit && n > 1)
                            isObsolete = true;

                        sumSize += fi.Length;
                        if (sumSize > 1024L * 1024L * _limits.TotalSizeLimit && n > 1)
                            isObsolete = true;

                        if (fi.LastWriteTime < lastDate && n > 1)
                            isObsolete = true;

                        if (isObsolete)
                            fullNameList.Add(fi.FullName);
                    }
                }

                foreach (string fullName in fullNameList)
                {
                    try
                    {
                        File.Delete(fullName);
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine($"Log file \"{fullName}\" deletion failed. {ex.GetExceptionDigest()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Log garbage collection failed. {ex.GetExceptionDigest()}");
            }
            finally
            {
                IsCleanUP = false;
            }
        }
    }
}
