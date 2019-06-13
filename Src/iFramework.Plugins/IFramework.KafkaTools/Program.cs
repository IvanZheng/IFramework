using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IFramework.AspNet;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity;
using Unity.Microsoft.DependencyInjection;
using ServiceProviderFactory = IFramework.DependencyInjection.ServiceProviderFactory;

namespace IFramework.KafkaTools
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

                           webBuilder.UseStartup<Startup>();
                       })
                       .UseServiceProviderFactory(new ServiceProviderFactory())
                       ;
        }
    }
}
