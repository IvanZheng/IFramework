using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace IFramework.Log4Net
{
    public class Log4NetProvider : ILoggerProvider
    {
        private readonly Func<object, Exception, string> _exceptionFormatter;
        private readonly string _log4NetConfigFile;
        private readonly ConcurrentDictionary<string, Log4NetLogger> loggers = new ConcurrentDictionary<string, Log4NetLogger>();

        public Log4NetProvider(string log4NetConfigFile, Func<object, Exception, string> exceptionFormatter)
        {
            _log4NetConfigFile = log4NetConfigFile;
            _exceptionFormatter = exceptionFormatter ?? FormatExceptionByDefault;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return loggers.GetOrAdd(categoryName, key => CreateLoggerImplementation(key, _exceptionFormatter));
        }

        public void Dispose()
        {
            loggers.Clear();
        }

        private static XmlElement Parselog4NetConfigFile(string filename)
        {
#if FULL_NET_FRAMEWORK
            filename = Path.Combine(AppDomain.CurrentDomain
                                             .BaseDirectory,
                                    filename);
#endif
            using (FileStream fileStream = File.OpenRead(filename))
            {
                XmlReaderSettings settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit
                };
                XmlDocument xmlDocument = new XmlDocument();
                using (XmlReader reader = XmlReader.Create(fileStream, settings))
                {
                    xmlDocument.Load(reader);
                }

                fileStream.Flush();
                fileStream.Dispose();
                return xmlDocument["log4net"];
            }
        }

        private Log4NetLogger CreateLoggerImplementation(string name, Func<object, Exception, string> exceptionFormatter)
        {
            return new Log4NetLogger(name, Parselog4NetConfigFile(_log4NetConfigFile)).UsingCustomExceptionFormatter(exceptionFormatter);
        }

        private static string FormatExceptionByDefault<TState>(TState state, Exception exception)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(state);
            stringBuilder.Append(" - ");
            if (exception != null)
            {
                stringBuilder.Append(exception);
            }

            return stringBuilder.ToString();
        }
    }
}