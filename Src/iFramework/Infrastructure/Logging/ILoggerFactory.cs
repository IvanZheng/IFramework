using System;

namespace IFramework.Infrastructure.Logging
{
    /// <summary>
    ///     Represents a logger factory.
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        /// Create a logger with the given logger name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level"></param>
        /// <param name="module"></param>
        /// <returns></returns>
        ILogger Create(string name, Level level = Level.Debug, object module = null);

        /// <summary>
        ///  Create a logger with the given type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <param name="module"></param>
        /// <returns></returns>
        ILogger Create(Type type, Level level = Level.Debug, object module = null);
    }
}