using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Logging
{
    public delegate void LoggerLevelChanged(string app, string logger, Level level);

    public interface ILoggerLevelController
    {
        void SetDefaultLevel(Level defaultLevel);
        Level GetOrAddLoggerLevel(string app, string name, Level? level);
        void SetLoggerLevel(string app, string name, Level level);
        void SetAppDefaultLevel(string app, Level level);
        event LoggerLevelChanged OnLoggerLevelChanged;
    }
}
