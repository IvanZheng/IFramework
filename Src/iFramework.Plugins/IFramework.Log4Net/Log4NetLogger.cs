using System;
using System.Collections.Generic;
using IFramework.Infrastructure;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using IFramework.Infrastructure.Logging;
using ILogger = IFramework.Infrastructure.Logging.ILogger;
using Level = log4net.Core.Level;

namespace IFramework.Log4Net
{
    /// <summary>
    ///     Log4Net based logger implementation.
    /// </summary>
    public class Log4NetLogger : ILogger
    {
        private readonly ILog _log;
        public Dictionary<string, object> AdditionalProperties { get; protected set; }
        public string App { get; protected set; }
        public string Module { get; protected set; }

        /// <summary>
        ///     Log4NetLogger constructor.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="level"></param>
        /// <param name="app"></param>
        /// <param name="module"></param>
        /// <param name="additionalProperties"></param>
        public Log4NetLogger(ILog log, 
                             Infrastructure.Logging.Level level = Infrastructure.Logging.Level.Debug, 
                             string app = null, 
                             string module = null, 
                             object additionalProperties = null)
        {
            _log = log;
            ChangeLogLevel(level);
            App = app;
            Module = module;
            SetAdditionalProperties(additionalProperties);
        }

        protected void SetAdditionalProperties(object additionalProperties)
        {
            var additionalDict = additionalProperties?.ToJson().ToJsonObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(App))
            {
                additionalDict[nameof(App)] = App;
            }
            if (!string.IsNullOrWhiteSpace(Module))
            {
                additionalDict[nameof(Module)] = Module;
            }
            AdditionalProperties = additionalDict;
        }
        void SetAdditionalProperties()
        {
            LogicalThreadContext.Properties[nameof(AdditionalProperties)] = AdditionalProperties;
        }

        #region ILogger Members

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public void Debug(object message)
        {
            SetAdditionalProperties();
            _log.Debug(message);
        }

        /// <summary>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void DebugFormat(string format, params object[] args)
        {
            SetAdditionalProperties();
            _log.DebugFormat(format, args);
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void Debug(object message, Exception exception)
        {
            SetAdditionalProperties();
            _log.Debug(message, exception);
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public void Info(object message)
        {
            SetAdditionalProperties();
            _log.Info(message);
        }

        /// <summary>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void InfoFormat(string format, params object[] args)
        {
            SetAdditionalProperties();
            _log.InfoFormat(format, args);
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void Info(object message, Exception exception)
        {
            SetAdditionalProperties();
            _log.Info(message, exception);
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public void Error(object message)
        {
            SetAdditionalProperties();
            _log.Error(message);
        }

        /// <summary>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void ErrorFormat(string format, params object[] args)
        {
            SetAdditionalProperties();
            _log.ErrorFormat(format, args);
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void Error(object message, Exception exception)
        {
            SetAdditionalProperties();
            _log.Error(message, exception);
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public void Warn(object message)
        {
            SetAdditionalProperties();
            _log.Warn(message);
        }

        /// <summary>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WarnFormat(string format, params object[] args)
        {
            SetAdditionalProperties();
            _log.WarnFormat(format, args);
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void Warn(object message, Exception exception)
        {
            SetAdditionalProperties();
            _log.Warn(message, exception);
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public void Fatal(object message)
        {
            SetAdditionalProperties();
            _log.Fatal(message);
        }

        /// <summary>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void FatalFormat(string format, params object[] args)
        {
            SetAdditionalProperties();
            _log.FatalFormat(format, args);
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void Fatal(object message, Exception exception)
        {
            SetAdditionalProperties();
            _log.Fatal(message, exception);
        }

        public void ChangeLogLevel(Infrastructure.Logging.Level level)
        {
            ((Logger)_log.Logger).Level = ToLog4NetLevel(level);
        }



        public static Level ToLog4NetLevel(Infrastructure.Logging.Level level)
        {
            var log4NetLevel = log4net.Core.Level.Debug;
            switch (level)
            {
                case Infrastructure.Logging.Level.All:
                    log4NetLevel = log4net.Core.Level.All;
                    break;
                case Infrastructure.Logging.Level.Debug:
                    log4NetLevel = log4net.Core.Level.Debug;
                    break;
                case Infrastructure.Logging.Level.Info:
                    log4NetLevel = log4net.Core.Level.Info;
                    break;
                case Infrastructure.Logging.Level.Warn:
                    log4NetLevel = log4net.Core.Level.Warn;
                    break;
                case Infrastructure.Logging.Level.Error:
                    log4NetLevel = log4net.Core.Level.Error;
                    break;
                case Infrastructure.Logging.Level.Fatal:
                    log4NetLevel = log4net.Core.Level.Fatal;
                    break;
            }
            return log4NetLevel;
        }

        public static Infrastructure.Logging.Level ToLoggingLevel(Level level)
        {
            var loggingLevel = Infrastructure.Logging.Level.Debug;
            if (level == log4net.Core.Level.All)
            {
                loggingLevel = Infrastructure.Logging.Level.All;
            }
            else if (level == log4net.Core.Level.Debug)
            {
                loggingLevel = Infrastructure.Logging.Level.Debug;
            }
            else if (level == log4net.Core.Level.Info)
            {
                loggingLevel = Infrastructure.Logging.Level.Info;
            }
            else if (level == log4net.Core.Level.Warn)
            {
                loggingLevel = Infrastructure.Logging.Level.Warn;
            }
            else if (level == log4net.Core.Level.Error)
            {
                loggingLevel = Infrastructure.Logging.Level.Error;
            }
            else if (level == log4net.Core.Level.Fatal)
            {
                loggingLevel = Infrastructure.Logging.Level.Fatal;
            }
            return loggingLevel;
        }

        public Infrastructure.Logging.Level Level => ToLoggingLevel(((Logger)_log.Logger).Level);

        #endregion
    }
}