using Sample.CommandService.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.CommandServiceTests
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var test = new CommandBusTests();
                test.Initialize();
                test.CommandBusPressureTest();
                Console.ReadLine();
                //test.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetBaseException().Message);
            }
            Console.WriteLine("exit!");

        }
    }
}
