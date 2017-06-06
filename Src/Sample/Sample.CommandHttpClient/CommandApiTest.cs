using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using IFramework.AspNet;

namespace Sample.CommandHttpClient
{
    [TestClass]

    public class CommandApiTest
    {
        public CommandApiTest(int batch)
        {
            this.batch = batch;
        }
        private int batch = 10;
        [TestMethod]
        public void ExecuteCommandTest()
        {
           
            
            var start = DateTime.Now;
            List<Task<string>> tasks= new List<Task<string>>();
            for (int i = 0; i < batch; i++)
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:2861")
                };
                var login = new Command.Login
                {
                    UserName = "ivan",
                    Password = "123456"
                };
                tasks.Add(client.DoCommand(login)
                                .ContinueWith(t =>
                                {
                                    return t.Result.Content
                                            .ReadAsStringAsync();
                                })
                                .Unwrap());
            }
            Task.WhenAll(tasks).Wait();
            Console.WriteLine(tasks.Last().Result);
            Console.WriteLine($"complete do commands cost:{(DateTime.Now - start).TotalMilliseconds}");
           
        }

    }
}
