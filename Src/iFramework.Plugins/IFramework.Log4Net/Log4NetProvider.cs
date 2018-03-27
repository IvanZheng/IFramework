using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using IFramework.Config;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4Net
{
    public class Log4NetProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, Log4NetLogger> _loggers = new ConcurrentDictionary<string, Log4NetLogger>();

        private readonly ILoggerRepository _loggerRepository;

        public Log4NetProvider(string log4NetConfigFile)
        {
            var configFile = GetLog4NetConfigFile(log4NetConfigFile);
            _loggerRepository = LogManager.CreateRepository(Configuration.GetAppSetting("app") ?? Assembly.GetCallingAssembly().FullName,
                                                            typeof(log4net.Repository.Hierarchy.Hierarchy));
            XmlConfigurator.ConfigureAndWatch(_loggerRepository, configFile);

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
            filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    filename);
            return new FileInfo(filename);
        }

 

        private Log4NetLogger CreateLoggerImplementation(string name)
        {
            return new Log4NetLogger(_loggerRepository.Name, name);
        }
    }
}