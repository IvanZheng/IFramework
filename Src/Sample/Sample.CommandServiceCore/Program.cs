using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Sample.CommandServiceCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                          .UseUrls(new ConfigurationBuilder().AddCommandLine(args).Build()["urls"])
                          .UseStartup<Startup>()
                          .Build();
        }
    }
}