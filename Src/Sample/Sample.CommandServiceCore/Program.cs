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
            var configRoot = new ConfigurationBuilder().AddCommandLine(args).Build();
            var urls = configRoot.GetValue("urls", string.Empty);
            var environment = configRoot.GetValue("environment", string.Empty);

            var build = WebHost.CreateDefaultBuilder(args);
            if (!string.IsNullOrWhiteSpace(urls))
            {
                build.UseUrls(urls);
            }
            if (!string.IsNullOrWhiteSpace(environment))
            {
                build.UseEnvironment(environment);
            }
            return build.UseStartup<Startup>()
                        .Build();
        }
    }
}