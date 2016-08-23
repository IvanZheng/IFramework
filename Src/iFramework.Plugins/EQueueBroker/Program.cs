using IFramework.Config;
using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EQueueBroker
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var brokerAddress = 
                Configuration.Instance
                         .UseAutofacContainer()
                         .UseEQueue(Utility.GetLocalIPV4().ToString())
                         .StartEqueueBroker();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetBaseException().Message);
            }
            Console.ReadLine();
        }
    }
}
