using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using log4net;
using log4net.Config;
using log4net.Repository;

namespace IFramework.Log4Net
{
    public class Log4NetLogger : ILogger
  {
    private readonly string name;
    private readonly XmlElement xmlElement;
    private readonly ILog log;
    private Func<object, Exception, string> exceptionDetailsFormatter;
    private ILoggerRepository loggerRepository;

    public Log4NetLogger(string name, XmlElement xmlElement)
    {
      this.name = name;
      this.xmlElement = xmlElement;
      this.loggerRepository = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof (log4net.Repository.Hierarchy.Hierarchy));
      this.log = LogManager.GetLogger(this.loggerRepository.Name, name);
      XmlConfigurator.Configure(loggerRepository, xmlElement);
    }

    public IDisposable BeginScope<TState>(TState state)
    {
      return (IDisposable) null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
      switch (logLevel)
      {
        case LogLevel.Trace:
        case LogLevel.Debug:
          return this.log.IsDebugEnabled;
        case LogLevel.Information:
          return this.log.IsInfoEnabled;
        case LogLevel.Warning:
          return this.log.IsWarnEnabled;
        case LogLevel.Error:
          return this.log.IsErrorEnabled;
        case LogLevel.Critical:
          return this.log.IsFatalEnabled;
        default:
          throw new ArgumentOutOfRangeException(nameof (logLevel));
      }
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
      if (!this.IsEnabled(logLevel))
        return;
      if (formatter == null)
        throw new ArgumentNullException(nameof (formatter));
      string str = (string) null;
      if (formatter != null)
        str = formatter(state, exception);
      if (exception != null && this.exceptionDetailsFormatter != null)
        str = this.exceptionDetailsFormatter((object) str, exception);
      if (string.IsNullOrEmpty(str) && exception == null)
        return;
      switch (logLevel)
      {
        case LogLevel.Trace:
        case LogLevel.Debug:
          this.log.Debug((object) str);
          break;
        case LogLevel.Information:
          this.log.Info((object) str);
          break;
        case LogLevel.Warning:
          this.log.Warn((object) str);
          break;
        case LogLevel.Error:
          this.log.Error((object) str);
          break;
        case LogLevel.Critical:
          this.log.Fatal((object) str);
          break;
        default:
          this.log.Warn((object) string.Format("Encountered unknown log level {0}, writing out as Info.", (object) logLevel));
          this.log.Info((object) str, exception);
          break;
      }
    }

    public Log4NetLogger UsingCustomExceptionFormatter(Func<object, Exception, string> formatter)
    {
      Func<object, Exception, string> func = formatter;
        this.exceptionDetailsFormatter = func ?? throw new ArgumentNullException(nameof (formatter));
      return this;
    }
  }
}
