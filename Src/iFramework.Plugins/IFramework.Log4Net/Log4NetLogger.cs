using System;
using IFramework.Infrastructure.Logging;
using log4net;
using log4net.Core;
using ILogger = IFramework.Infrastructure.Logging.ILogger;

namespace IFramework.Log4Net
{
    /// <summary>
    ///     Log4Net based logger implementation.
    /// </summary>
    public class Log4NetLogger: ILogger
    {
        private readonly ILog _log;
        private readonly ILoggerLevelController _loggerLevelController;

        /// <summary>
        ///     Parameterized constructor.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="loggerLevelController"></param>
        public Log4NetLogger(ILog log, ILoggerLevelController loggerLevelController)
        {
            _log = log;
            _loggerLevelController = loggerLevelController;
        }

        #region ILogger Members

        private Level CurrentLevel => (Level)_loggerLevelController.GetLoggerLevel(_log.Logger.Name) ?? Level.All;

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public void Debug(object message)
        {
            if (Level.Debug >= CurrentLevel)
            {
                _log.Debug(message);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void DebugFormat(string format, params object[] args)
        {
            if (Level.Debug >= CurrentLevel)
            {
                _log.DebugFormat(format, args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void Debug(object message, Exception exception)
        {
            if (Level.Debug >= CurrentLevel)
            {
                _log.Debug(message, exception);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public void Info(object message)
        {
            if (Level.Info >= CurrentLevel)
            {
                _log.Info(message);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void InfoFormat(string format, params object[] args)
        {
            if (Level.Info >= CurrentLevel)
            {
                _log.InfoFormat(format, args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void Info(object message, Exception exception)
        {
            if (Level.Info >= CurrentLevel)
            {
                _log.Info(message, exception);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public void Error(object message)
        {
            if (Level.Error >= CurrentLevel)
            {
                _log.Error(message);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void ErrorFormat(string format, params object[] args)
        {
            if (Level.Error >= CurrentLevel)
            {
                _log.ErrorFormat(format, args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void Error(object message, Exception exception)
        {
            if (Level.Error >= CurrentLevel)
            {
                _log.Error(message, exception);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public void Warn(object message)
        {
            if (Level.Warn >= CurrentLevel)
            {
                _log.Warn(message);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WarnFormat(string format, params object[] args)
        {
            if (Level.Warn >= CurrentLevel)
            {
                _log.WarnFormat(format, args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void Warn(object message, Exception exception)
        {
            if (Level.Warn >= CurrentLevel)
            {
                _log.Warn(message, exception);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        public void Fatal(object message)
        {
            if (Level.Fatal >= CurrentLevel)
            {
                _log.Fatal(message);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void FatalFormat(string format, params object[] args)
        {
            if (Level.Fatal >= CurrentLevel)
            {
                _log.FatalFormat(format, args);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void Fatal(object message, Exception exception)
        {
            if (Level.Fatal >= CurrentLevel)
            {
                _log.Fatal(message, exception);
            }
        }

        public void ChangeLogLevel(object level)
        {
            (_log.Logger as log4net.Repository.Hierarchy.Logger).Level = (Level) level;
        }

        #endregion
    }
}