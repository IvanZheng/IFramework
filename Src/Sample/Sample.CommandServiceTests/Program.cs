using System;
using Sample.CommandService.Tests;

namespace Sample.CommandServiceTests
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var test = new CommandBusTests();
                test.Initialize();
                test.CommandBusPressureTest();
                //test.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetBaseException().Message);
            }
        }
    }
}