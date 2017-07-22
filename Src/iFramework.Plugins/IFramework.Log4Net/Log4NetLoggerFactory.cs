using System;
using System.Collections.Concurrent;
using System.IO;
using IFramework.Infrastructure.Logging;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;

namespace IFramework.Log4Net
{
    /// <summary>
    ///     Log4Net based logger factory.
    /// </summary>
    public class Log4NetLoggerFactory : ILoggerFactory
    {
        static readonly ConcurrentDictionary<string, ILogger> Loggers = new ConcurrentDictionary<string, ILogger>();
        /// <summary>
        ///     Parameterized constructor.
        /// </summary>
        /// <param name="configFile"></param>
        public Log4NetLoggerFactory(string configFile)
        {
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
                BasicConfigurator.Configure(new TraceAppender {Layout = new PatternLayout()});
            }
        }

        /// <summary>
        ///     Create a new Log4NetLogger instance.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ILogger Create(string name)
        {
            return Loggers.GetOrAdd(name, key => new Log4NetLogger(LogManager.GetLogger(key)));
        }

        /// <summary>
        ///     Create a new Log4NetLogger instance.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ILogger Create(Type type)
        {
            return Create(type.FullName);
        }
    }
}