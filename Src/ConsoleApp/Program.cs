using System;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            while (true)
            {
                Console.WriteLine($"It's {DateTime.Now}");
                Task.Delay(TimeSpan.FromDays(1)).Wait();
            }
        }
    }
}
