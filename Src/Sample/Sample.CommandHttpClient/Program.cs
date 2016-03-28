using Sample.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.AspNet;

namespace Sample.CommandHttpClient
{
    public class ModifyCooperatorBasic : ICommand
    {
        public string CooperatorID { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public string Remark { get; set; }

        public bool NeedRetry
        {
            get; set;
        }

        public string ID
        {
            get; set;
        }

        public ModifyCooperatorBasic() { }
        
    }
    class Program
    {
        static void Main(string[] args)
        {
            var apiClient = new HttpClient();
            // apiClient.BaseAddress = new System.Uri("http://localhost:6357");
            apiClient.BaseAddress = new System.Uri("http://localhost:18193");

            var command = new ModifyCooperatorBasic
            {
                CooperatorID = "52b007cd9a37601480b6d5e6",
                Name = "ivan",
                Type = 0,
                Remark = ""
            };

            var task = apiClient.DoCommand(command);

            var result = task.Result.Content.ReadAsStringAsync().Result;
            Console.Write(result);
        }
    }
}
