using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Logging
{
    public class LoggerLevelController: ILoggerLevelController
    {
        protected object DefaultLoggerLevel { get; set; }
        protected static ConcurrentDictionary<string, object> LoggerLevels = new ConcurrentDictionary<string, object>();
        public virtual object GetLoggerLevel(string name)
        {
            return LoggerLevels.TryGetValue(name, DefaultLoggerLevel);
        }

        public virtual void SetLoggerLevel(string name, object level)
        {
            LoggerLevels.AddOrUpdate(name, key => level, (key, value) => level);
        }

        public void SetDefaultLoggerLevel(object level)
        {
            DefaultLoggerLevel = level;
        }
    }
}
