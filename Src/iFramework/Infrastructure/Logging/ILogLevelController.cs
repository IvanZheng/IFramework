using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Logging
{
    public interface ILogLevelController
    {
        object GetLoggerLevel(string name);
        void RegisterLogger(string name);
    }
}
