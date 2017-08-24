using System;
using System.Collections.Concurrent;
using System.IO;
using IFramework.Config;
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
        private readonly string _defaultApp;

        /// <summary>
        ///     Parameterized constructor.
        /// </summary>
        /// <param name="configFile"></param>
        /// <param name="loggerLevelController"></param>
        /// <param name="defaultApp"></param>
        /// <param name="defaultLevel"></param>
        public Log4NetLoggerFactory(string configFile, ILoggerLevelController loggerLevelController, string defaultApp, Level defaultLevel = Level.Debug)
        {
            _defaultApp = defaultApp;
            _loggerLevelController = loggerLevelController;
            _loggerLevelController.SetDefaultLevel(defaultLevel);
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

        private void _loggerLevelController_OnLoggerLevelChanged(string app, string logger, Level level)
        {
            Loggers.TryGetValue(logger, null)?.ChangeLogLevel(level);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level"></param>
        /// <param name="app"></param>
        /// <param name="module"></param>
        /// <param name="additionalProperties"></param>
        /// <returns></returns>
        public ILogger Create(string name, string app = null, string module = null, Level? level = null, object additionalProperties = null)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            app = app ?? _defaultApp;
            var loggerKey = $"{app}{name}";
            var logger = Loggers.GetOrAdd(loggerKey, key => new Log4NetLogger(LogManager.GetLogger(key),
                                                                              _loggerLevelController.GetOrAddLoggerLevel(app, name, level),
                                                                              app,
                                                                              module,
                                                                              additionalProperties));
            return logger;
        }

        /// <summary>
        ///     Create a new Log4NetLogger instance.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <param name="additionalProperties"></param>
        /// <returns></returns>
        public ILogger Create(Type type, Level? level = null, object additionalProperties = null)
        {
            return Create(type.FullName, null, null, level, additionalProperties);
        }
    }
}