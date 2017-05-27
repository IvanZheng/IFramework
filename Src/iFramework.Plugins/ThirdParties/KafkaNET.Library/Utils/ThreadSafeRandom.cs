using System;

namespace Kafka.Client.Utils
{
    public class ThreadSafeRandom
    {
        private readonly Random random = new Random();
        private readonly object useLock = new object();

        public int Next(int ceiling)
        {
            lock (useLock)
            {
                return random.Next(ceiling);
            }
        }

        public int Next()
        {
            lock (useLock)
            {
                return random.Next();
            }
        }
    }
}