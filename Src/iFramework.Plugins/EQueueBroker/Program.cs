using System;
using IFramework.Config;

namespace EQueueBroker
{
    public class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Configuration.Instance
                             .UseAutofacContainer()
                             .UseEQueue()
                             .StartEqueueBroker();
                Console.WriteLine("EQueue Broker started.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetBaseException().Message);
            }
            Console.ReadLine();
        }
    }
}