using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Logging
{
    public class LoggerLevelController : ILoggerLevelController
    {
        protected Level DefaultLevel { get; set; }
        protected static ConcurrentDictionary<string, Level> LoggerLevels = new ConcurrentDictionary<string, Level>();

        public virtual Level GetOrAddLoggerLevel(string name, Level? level = null)
        {
            return LoggerLevels.GetOrAdd(name, key => level ?? DefaultLevel);
        }

        public virtual void SetLoggerLevel(string name, Level? level = null)
        {
            level = LoggerLevels.AddOrUpdate(name,
                                             key => level ?? DefaultLevel,
                                             (key, value) => level ?? DefaultLevel);
            OnLoggerLevelChanged?.Invoke(name, level.Value);
        }

        public void SetDefaultLoggerLevel(Level level)
        {
            DefaultLevel = level;
        }

        public event LoggerLevelChanged OnLoggerLevelChanged;
    }
}
