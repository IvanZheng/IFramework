using System;

namespace IFramework.Infrastructure.Logging
{
    internal class MockLoggerFactory : ILoggerFactory
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