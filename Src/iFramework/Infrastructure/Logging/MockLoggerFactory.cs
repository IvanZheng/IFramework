using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Logging
{
    class MockLoggerFactory : ILoggerFactory
    {
        public ILogger Create(Type type)
        {
            return MockLogger.Instance;
        }

        public ILogger Create(string name)
        {
            return MockLogger.Instance;
        }
    }
}
