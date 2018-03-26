using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4Net
{
    public class Log4NetProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, Log4NetLogger> _loggers = new ConcurrentDictionary<string, Log4NetLogger>();
#if !FULL_NET_FRAMEWORK
        private readonly ILoggerRepository _loggerRepository;
#endif
        public Log4NetProvider(string log4NetConfigFile)
        {
            var configFile = GetLog4NetConfigFile(log4NetConfigFile);
#if FULL_NET_FRAMEWORK
            XmlConfigurator.ConfigureAndWatch(configFile);
#else
            _loggerRepository = LogManager.CreateRepository(Assembly.GetEntryAssembly(),
                                                            typeof(log4net.Repository.Hierarchy.Hierarchy));
            XmlConfigurator.ConfigureAndWatch(_loggerRepository, configFile);
#endif
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
        }

        public void Dispose()
        {
            _loggers.Clear();
        }

        private static FileInfo GetLog4NetConfigFile(string filename)
        {
#if FULL_NET_FRAMEWORK
            filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    filename);
#endif
            return new FileInfo(filename);
        }

 

        private Log4NetLogger CreateLoggerImplementation(string name)
        {
#if FULL_NET_FRAMEWORK
            return new Log4NetLogger(name);
#else
            return new Log4NetLogger(_loggerRepository.Name, name);
#endif
        }
    }
}