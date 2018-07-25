using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleTest
{
    internal class Program
    {
        private static readonly HttpClient HttpClient = new HttpClient {BaseAddress = new Uri("https://www.baidu.com") };

        private static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(2, 200);
            ThreadPool.SetMaxThreads(5, 200);
            ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
            Console.WriteLine($"workerThreads: {workerThreads} completionPortThreads: {completionPortThreads}");

            var batch = 5;
            var tasks = new List<Task<string>>();
            for (var i = 0; i < batch; i++)
            {
                tasks.Add(Task.Run(DoTaskAsync));
            }
            int j = 0;
            foreach (var task in tasks)
            {
                var result = task.Result;
                Console.WriteLine($"{result} {j++}");
            }
            Console.ReadLine();
        }

        private static async Task<string> DoTaskAsync()
        {
            await Task.Delay(2000);
            return "done";

        }

        private static Task<string> DoIOTaskAsync()
        {
            return HttpClient.GetAsync($"api/command?wd={DateTime.Now.ToShortDateString()}")
                              .Result
                              .Content
                              .ReadAsStringAsync();
        }
    }
}