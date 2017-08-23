using System;
using System.Collections.Concurrent;
using System.IO;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using IFramework.Infrastructure.Logging;
using IFramework.Infrastructure;

namespace IFramework.Log4Net
{
    /// <summary>
    ///     Log4Net based logger factory.
    /// </summary>
    public class Log4NetLoggerFactory : ILoggerFactory
    {
        private readonly ILoggerLevelController _loggerLevelController;
        static readonly ConcurrentDictionary<string, ILogger> Loggers = new ConcurrentDictionary<string, ILogger>();

        /// <summary>
        ///     Parameterized constructor.
        /// </summary>
        /// <param name="configFile"></param>
        /// <param name="loggerLevelController"></param>
        /// <param name="defaultLevel"></param>
        public Log4NetLoggerFactory(string configFile, ILoggerLevelController loggerLevelController, Level defaultLevel = Level.Debug)
        {
            _loggerLevelController = loggerLevelController;
            _loggerLevelController.SetDefaultLoggerLevel(defaultLevel);
            _loggerLevelController.OnLoggerLevelChanged += _loggerLevelController_OnLoggerLevelChanged;
            var file = new FileInfo(configFile);
            if (!file.Exists)
            {
                file = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFile));
            }

            if (file.Exists)
            {
                XmlConfigurator.ConfigureAndWatch(file);
            }
            else
            {
                BasicConfigurator.Configure(new TraceAppender { Layout = new PatternLayout() });
            }
        }

        private void _loggerLevelController_OnLoggerLevelChanged(string logger, Level level)
        {
            Loggers.TryGetValue(logger, null)?.ChangeLogLevel(level);
        }

        /// <summary>
        ///     Create a new Log4NetLogger instance.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level"></param>
        /// <param name="module"></param>
        /// <returns></returns>
        public ILogger Create(string name, Level level = Level.Debug, object module = null)
        {
            var logger = Loggers.GetOrAdd(name, key => new Log4NetLogger(LogManager.GetLogger(key),
                                                                         _loggerLevelController.GetOrAddLoggerLevel(key, level),
                                                                         module));
            logger.SetModule(module);
            return logger;
        }

        /// <summary>
        ///     Create a new Log4NetLogger instance.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="module"></param>
        /// <returns></returns>
        public ILogger Create(Type type, Level level = Level.Debug, object module = null)
        {
            return Create(type.FullName, level, module);
        }
    }
}