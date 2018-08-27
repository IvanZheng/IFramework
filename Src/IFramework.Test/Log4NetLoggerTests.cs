using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.Log4Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using IFramework.Infrastructure;

namespace IFramework.Test
{
    public class Log4NetLoggerTests
    {
        public Log4NetLoggerTests()
        {
        }

        [Fact]
        public void TestLog()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            
            Configuration.Instance
                         .UseAutofacContainer(new ContainerBuilder())
                         .UseConfiguration(builder.Build())
                         .UseLog4Net();

            ObjectProviderFactory.Instance.Build();

            var loggerFactory = ObjectProviderFactory.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(GetType());


            try
            {
                throw new NotImplementedException("test exception");
            }
            catch (Exception e)
            {
                LogTest(logger, e);
            }
        }

        void LogTest(ILogger logger, object message)
        {
            logger.LogDebug(message);
            logger.LogInformation(message);
            logger.LogWarning(message);
            logger.LogError(message);
            logger.LogCritical(message);
        }
    }
}
