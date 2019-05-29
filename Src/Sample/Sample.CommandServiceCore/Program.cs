using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sample.CommandServiceCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                       .ConfigureWebHostDefaults(webBuilder =>
                       {
                           var configRoot = new ConfigurationBuilder().AddCommandLine(args).Build();
                           var urls = configRoot.GetValue("urls", string.Empty);
                           var environment = configRoot.GetValue("environment", string.Empty);
                           if (!string.IsNullOrWhiteSpace(urls))
                           {
                               webBuilder.UseUrls(urls);
                           }

                           if (!string.IsNullOrWhiteSpace(environment))
                           {
                               webBuilder.UseEnvironment(environment);
                           }

                           webBuilder.ConfigureServices(services => services.AddAutofac())
                                     .UseStartup<Startup>();
                       });
        }
    }
}