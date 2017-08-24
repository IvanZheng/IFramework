using System;

namespace IFramework.Infrastructure.Logging
{
    internal class MockLoggerFactory : ILoggerFactory
    {
        public ILogger Create(string name, Level level = Level.Debug, object additionalProperties = null)
        {
            return MockLogger.Instance;
        }

        public ILogger Create(Type type, Level level = Level.Debug, object additionalProperties = null)
        {
            return MockLogger.Instance;
        }
    }
}