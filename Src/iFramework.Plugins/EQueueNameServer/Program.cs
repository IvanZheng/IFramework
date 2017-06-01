using System;
using IFramework.Config;

namespace EQueueNameServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Configuration.Instance
                         .UseAutofacContainer()
                         .UseEQueue()
                         .StartEqueueNameServer();

            Console.WriteLine("Equeue name server started.");
            Console.ReadLine();
        }
    }
}