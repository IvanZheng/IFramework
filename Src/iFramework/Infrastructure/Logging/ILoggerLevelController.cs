using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Logging
{
    public delegate void LoggerLevelChanged(string logger, Level level);

    public interface ILoggerLevelController
    {
        Level GetOrAddLoggerLevel(string name, Level? level = null);
        void SetLoggerLevel(string name, Level? level = null);
        void SetDefaultLoggerLevel(Level level);
        event LoggerLevelChanged OnLoggerLevelChanged;
    }
}
