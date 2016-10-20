using IFramework.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EQueueNameServer
{
    class Program
    {
        static void Main(string[] args)
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
