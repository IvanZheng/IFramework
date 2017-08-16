using System;

namespace IFramework.Infrastructure.Logging
{
    /// <summary>
    ///     Represents a logger factory.
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        ///     Create a logger with the given logger name.
        /// </summary>
        ILogger Create(string name, Level level = Level.Debug);

        /// <summary>
        ///     Create a logger with the given type.
        /// </summary>
        ILogger Create(Type type, Level level = Level.Debug);
    }
}