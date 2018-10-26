using System;
using System.Diagnostics;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.Log4Net;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4NetTests
{
    [TestClass]
    [DeploymentItem("log4net.config", "")]
    [DeploymentItem("appsettings.json", "")]
    public class Log4NetLoggerTests
    {
        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance
                         .UseAutofacContainer(System.Reflection.Assembly.GetExecutingAssembly().FullName)
                         .UseConfiguration(new ConfigurationBuilder().Build())
                         .UseCommonComponents()
                         .UseLog4Net();
            ObjectProviderFactory.Instance.Build();
        }

        [TestMethod]
        public void TestLog()
        {
            var loggerFactory = IoCFactory.GetService<ILoggerFactory>();
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
