using System;

namespace IFramework.Infrastructure.Logging
{
    internal class MockLoggerFactory : ILoggerFactory
    {
        public ILogger Create(Type type, object level = null)
        {
            return MockLogger.Instance;
        }

        public ILogger Create(string name, object level = null)
        {
            return MockLogger.Instance;
        }
    }
}