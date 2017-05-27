using System;
using System.Diagnostics;

namespace Kafka.Client
{
    public static class KafkaStopWatch
    {
        private static Stopwatch stopWatch;
        private static long memUsage;

        public static void Start()
        {
            stopWatch = Stopwatch.StartNew();
            memUsage = GC.GetTotalMemory(false);
        }

        public static void Checkpoint(string msg)
        {
            stopWatch.Stop();
            var current = GC.GetTotalMemory(false) - memUsage;
            var result = stopWatch.Elapsed.TotalMilliseconds;
            Console.WriteLine("{0}: {1,-6:0.000}ms, {2, 10:0.0}kB, {3, 10:0.0}kB", msg, result,
                GC.GetTotalMemory(false) / 1000.0, current / 1000.0);
            memUsage = GC.GetTotalMemory(false);
            stopWatch.Restart();
        }
    }
}