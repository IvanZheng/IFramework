using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using IFramework.Config;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4Net
{
    public class Log4NetProvider : ILoggerProvider
    {
        private readonly Log4NetProviderOptions _options;
        private readonly ILoggerRepository _loggerRepository;
        private readonly ConcurrentDictionary<string, Log4NetLogger> _loggers = new ConcurrentDictionary<string, Log4NetLogger>();

        public Log4NetProvider(Log4NetProviderOptions options)
        {
            _options = options ?? Log4NetProviderOptions.Default;
            var configFile = GetLog4NetConfigFile(_options.ConfigFile);

            var repositoryName = Configuration.Get("app") ?? Assembly.GetCallingAssembly()
                                                                     .FullName;
            _loggerRepository = LogManager.GetAllRepositories()
                                          .FirstOrDefault(r => r.Name == repositoryName) ?? LogManager.CreateRepository(repositoryName);
            XmlConfigurator.ConfigureAndWatch(_loggerRepository, configFile);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, key => CreateLoggerImplementation(key, _options));
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


        private Log4NetLogger CreateLoggerImplementation(string name, Log4NetProviderOptions options)
        {
            return new Log4NetLogger(_loggerRepository.Name, name, options);
        }
    }
}