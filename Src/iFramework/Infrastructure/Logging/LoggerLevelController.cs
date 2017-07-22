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
        protected static ConcurrentDictionary<string, object> LoggerLevels = new ConcurrentDictionary<string, object>();
        public virtual object GetLoggerLevel(string name)
        {
            return LoggerLevels.TryGetValue(name, null);
        }

        public virtual void SetLoggerLevel(string name, object level)
        {
            LoggerLevels.AddOrUpdate(name, key => level, (key, value) => level);
        }
    }
}
