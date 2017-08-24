using System;
using System.Diagnostics;
using IFramework.Config;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using log4netCore = log4net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IFramework.Log4NetTests
{
    [TestClass]
    [DeploymentItem("log4net.config", "")]
    public class Log4NetLoggerTests
    {
        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance
                         //.UseAutofacContainer()
                         //.RegisterAssemblyTypes(System.Reflection.Assembly.GetExecutingAssembly().FullName)
                         .UseUnityContainer()
                         .RegisterCommonComponents()
                         .UseLog4Net("Log4NetLoggerTest");
        }

        [TestMethod]
        public void TestLog()
        {
            var loggerFactory = IoCFactory.Resolve<ILoggerFactory>();
            var logger = loggerFactory.Create(nameof(Log4NetLoggerTests));
            var message = "test log level";
            Console.WriteLine(logger.Level);
            LogTest(logger, message);

            logger.ChangeLogLevel(Level.Debug);
            Console.WriteLine(logger.Level);
            LogTest(logger, message);

            logger.ChangeLogLevel(Level.Info);
            Console.WriteLine(logger.Level);
            LogTest(logger, message);

            logger.ChangeLogLevel(Level.Warn);
            Console.WriteLine(logger.Level);
            LogTest(logger, message);

            logger.ChangeLogLevel(Level.Error);
            Console.WriteLine(logger.Level);
            LogTest(logger, message);

            logger.ChangeLogLevel(Level.Fatal);
            Console.WriteLine(logger.Level);
            LogTest(logger, message);
        }

        void LogTest(ILogger logger, string message)
        {
            logger.Debug(message);
            logger.Info(message);
            logger.Warn(message);
            logger.Error(message);
            logger.Fatal(message);
        }
    }
}
