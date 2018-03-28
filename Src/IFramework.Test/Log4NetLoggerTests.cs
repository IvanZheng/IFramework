using System.IO;
using Autofac;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.Log4Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IFramework.Test
{
    public class Log4NetLoggerTests
    {
        public Log4NetLoggerTests()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            
            Configuration.Instance
                         .UseConfiguration(builder.Build())
                         .UseAutofacContainer(new ContainerBuilder())
                         .UseLog4Net();
            ObjectProviderFactory.Instance.Build();
        }

        [Fact]
        public void TestLog()
        {
            var loggerFactory = ObjectProviderFactory.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(nameof(Log4NetLoggerTests));
            var message = "test log level";
            LogTest(logger, message);
        }

        void LogTest(ILogger logger, string message)
        {
            logger.LogDebug(message);
            logger.LogInformation(message);
            logger.LogWarning(message);
            logger.LogError(message);
            logger.LogCritical(message);
        }
    }
}
