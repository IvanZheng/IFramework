using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Logging
{
    public interface ILoggerLevelController
    {
        object GetLoggerLevel(string name);
        void SetLoggerLevel(string name, object level);
    }
}
