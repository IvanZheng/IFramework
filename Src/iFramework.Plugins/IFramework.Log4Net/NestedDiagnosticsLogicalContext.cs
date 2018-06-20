using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace IFramework.Log4Net
{
    internal static class ArrayHelper
    {
        internal static T[] Empty<T>()
        {
            return EmptyArray<T>.Instance;
        }

        private static class EmptyArray<T>
        {
            internal static readonly T[] Instance = new T[0];
        }
    }

    internal static class FormatHelper
    {
        /// <summary>Convert object to string</summary>
        /// <param name="o">value</param>
        /// <param name="formatProvider">format for conversion.</param>
        /// <returns></returns>
        /// <remarks>
        ///     If <paramref name="formatProvider" /> is <c>null</c> and <paramref name="o" /> isn't a
        ///     <see cref="T:System.String" /> already, then the <see cref="T:NLog.LogFactory" /> will get a locked by
        ///     <see cref="P:NLog.LogManager.Configuration" />
        /// </remarks>
        internal static string ConvertToString(object o, IFormatProvider formatProvider)
        {
            if (formatProvider == null && !(o is string))
            {
                formatProvider = CultureInfo.DefaultThreadCurrentCulture;
            }
            return Convert.ToString(o, formatProvider);
        }
    }

    /// <summary>
    ///     Async version of <see cref="T:NLog.NestedDiagnosticsContext" /> - a logical context structure that keeps a stack
    ///     Allows for maintaining scope across asynchronous tasks and call contexts.
    /// </summary>
    public static class NestedDiagnosticsLogicalContext
    {
        private static readonly AsyncLocal<INestedContext> AsyncNestedDiagnosticsContext = new AsyncLocal<INestedContext>();

        /// <summary>Pushes the specified value on current stack</summary>
        /// <param name="value">The value to be pushed.</param>
        /// <returns>
        ///     An instance of the object that implements IDisposable that returns the stack to the previous level when
        ///     IDisposable.Dispose() is called. To be used with C# using() statement.
        /// </returns>
        public static IDisposable Push<T>(T value)
        {
            var nestedContext = new NestedContext<T>(GetThreadLocal(), value);
            SetThreadLocal(nestedContext);
            return nestedContext;
        }

        /// <summary>Pops the top message off the NDLC stack.</summary>
        /// <returns>The top message which is no longer on the stack.</returns>
        /// <remarks>this methods returns a object instead of string, this because of backwardscompatibility</remarks>
        public static object Pop()
        {
            return PopObject();
        }

        /// <summary>Pops the top message from the NDLC stack.</summary>
        /// <param name="formatProvider">The <see cref="T:System.IFormatProvider" /> to use when converting the value to a string.</param>
        /// <returns>The top message, which is removed from the stack, as a string value.</returns>
        public static string Pop(IFormatProvider formatProvider)
        {
            return FormatHelper.ConvertToString(PopObject() ?? string.Empty, formatProvider);
        }

        /// <summary>Pops the top message off the current NDLC stack</summary>
        /// <returns>The object from the top of the NDLC stack, if defined; otherwise <c>null</c>.</returns>
        public static object PopObject()
        {
            var threadLocal = GetThreadLocal();
            if (threadLocal != null)
            {
                SetThreadLocal(threadLocal.Parent);
            }
            return threadLocal?.Value;
        }

        /// <summary>Peeks the top object on the current NDLC stack</summary>
        /// <returns>The object from the top of the NDLC stack, if defined; otherwise <c>null</c>.</returns>
        public static object PeekObject()
        {
            var nestedContext = PeekContext(false);
            return nestedContext?.Value;
        }

        /// <summary>Peeks the current scope, and returns its start time</summary>
        /// <returns>Scope Creation Time</returns>
        internal static DateTime PeekTopScopeBeginTime()
        {
            var nestedContext = PeekContext(false);
            return new DateTime(nestedContext?.CreatedTimeUtcTicks ?? DateTime.MinValue.Ticks, DateTimeKind.Utc);
        }

        /// <summary>Peeks the first scope, and returns its start time</summary>
        /// <returns>Scope Creation Time</returns>
        internal static DateTime PeekBottomScopeBeginTime()
        {
            var nestedContext = PeekContext(true);
            return new DateTime(nestedContext?.CreatedTimeUtcTicks ?? DateTime.MinValue.Ticks, DateTimeKind.Utc);
        }

        private static INestedContext PeekContext(bool bottomScope)
        {
            var nestedContext = GetThreadLocal();
            if (nestedContext == null)
            {
                return null;
            }
            if (bottomScope)
            {
                while (nestedContext.Parent != null)
                {
                    nestedContext = nestedContext.Parent;
                }
            }
            return nestedContext;
        }

        /// <summary>Clears current stack.</summary>
        public static void Clear()
        {
            SetThreadLocal(null);
        }

        /// <summary>Gets all messages on the stack.</summary>
        /// <returns>Array of strings on the stack.</returns>
        public static string[] GetAllMessages()
        {
            return GetAllMessages(null);
        }

        /// <summary>
        ///     Gets all messages from the stack, without removing them.
        /// </summary>
        /// <param name="formatProvider">The <see cref="T:System.IFormatProvider" /> to use when converting a value to a string.</param>
        /// <returns>Array of strings.</returns>
        public static string[] GetAllMessages(IFormatProvider formatProvider)
        {
            return GetAllObjects().Select(o => FormatHelper.ConvertToString(o, formatProvider)).Reverse().ToArray();
        }

        /// <summary>
        ///     Gets all objects on the stack. The objects are not removed from the stack.
        /// </summary>
        /// <returns>Array of objects on the stack.</returns>
        public static object[] GetAllObjects()
        {
            var nestedContext = GetThreadLocal();
            if (nestedContext == null)
            {
                return ArrayHelper.Empty<object>();
            }
            var num = 0;
            var objArray = new object[nestedContext.FrameLevel];
            for (; nestedContext != null; nestedContext = nestedContext.Parent)
            {
                objArray[num++] = nestedContext.Value;
            }
            return objArray;
        }

        private static void SetThreadLocal(INestedContext newValue)
        {
            AsyncNestedDiagnosticsContext.Value = newValue;
        }

        private static INestedContext GetThreadLocal()
        {
            return AsyncNestedDiagnosticsContext.Value;
        }

        private interface INestedContext : IDisposable
        {
            INestedContext Parent { get; }

            int FrameLevel { get; }

            object Value { get; }

            long CreatedTimeUtcTicks { get; }
        }

        private class NestedContext<T> : INestedContext, IDisposable
        {
            private int _disposed;

            public NestedContext(INestedContext parent, T value)
            {
                Parent = parent;
                Value = value;
                CreatedTimeUtcTicks = DateTime.UtcNow.Ticks;
                FrameLevel = parent?.FrameLevel + 1 ?? 1;
            }

            public T Value { get; }

            public INestedContext Parent { get; }

            object INestedContext.Value => Value;

            public long CreatedTimeUtcTicks { get; }

            public int FrameLevel { get; }

            void IDisposable.Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 1)
                {
                    return;
                }
                PopObject();
            }

            public override string ToString()
            {
                // ISSUE: variable of a boxed type
                var local = (object) Value;
                return local?.ToString() ?? "null";
            }
        }
    }
}