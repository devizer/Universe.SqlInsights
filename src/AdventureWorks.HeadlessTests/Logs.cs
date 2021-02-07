using System;
using System.Diagnostics;

namespace AdventureWorks.HeadlessTests
{
    class Logs
    {
        static Lazy<Stopwatch> StartAt = new Lazy<Stopwatch>(() => Stopwatch.StartNew());

        private static string PrevAt = null;
        static object Sync = new object();
        public static void Info(string msg)
        {
            var timer = StartAt.Value.ElapsedMilliseconds;
            var at = (new DateTime(0) + TimeSpan.FromMilliseconds(timer)).ToString("HH:mm:ss.fff");
            lock (Sync)
            {
                var fore = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(at == PrevAt ? new string(' ', at.Length) : at);
                // PrevAt = at;
                Console.ForegroundColor = fore;
                Console.WriteLine(" " + msg);
            }
        }
    }
}