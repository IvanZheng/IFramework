using IFramework.Config;
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
                         .UseEQueue("192.169.199.242")
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
