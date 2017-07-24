using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Logging
{
    public class LogLevelController: ILogLevelController
    {
        private static readonly ConcurrentDictionary<string, object> LoggerLevels = new ConcurrentDictionary<string, object>();

        public object GetLoggerLevel(string name)
        {
            return LoggerLevels.TryGetValue(name, -1);
        }

        public void RegisterLogger(string name)
        {
            throw new NotImplementedException();
        }
    }
}
