using System;
using System.Collections.Generic;
using System.Threading;

namespace IFramework.Log4Net
{
    /// <summary>
    /// Async version of Mapped Diagnostics Context - a logical context structure that keeps a dictionary
    /// of strings and provides methods to output them in layouts.  Allows for maintaining state across
    /// asynchronous tasks and call contexts.
    /// </summary>
    /// <remarks>
    /// Ideally, these changes should be incorporated as a new version of the MappedDiagnosticsContext class in the original
    /// NLog library so that state can be maintained for multiple threads in asynchronous situations.
    /// </remarks>
    public static class MappedDiagnosticsLogicalContext
    {
        private static readonly AsyncLocal<IDictionary<string, object>> AsyncLocalDictionary = new AsyncLocal<IDictionary<string, object>>();
        private static readonly IDictionary<string, object> EmptyDefaultDictionary = (IDictionary<string, object>)new SortHelpers.ReadOnlySingleBucketDictionary<string, object>();

        /// <summary>
        /// Simulate ImmutableDictionary behavior (which is not yet part of all .NET frameworks).
        /// In future the real ImmutableDictionary could be used here to minimize memory usage and copying time.
        /// </summary>
        /// <param name="clone">Must be true for any subsequent dictionary modification operation</param>
        /// <returns></returns>
        private static IDictionary<string, object> GetLogicalThreadDictionary(bool clone = false)
        {
            IDictionary<string, object> newValue = MappedDiagnosticsLogicalContext.GetThreadLocal();
            if (newValue == null)
            {
                if (!clone)
                    return MappedDiagnosticsLogicalContext.EmptyDefaultDictionary;
                newValue = (IDictionary<string, object>)new Dictionary<string, object>();
                MappedDiagnosticsLogicalContext.SetThreadLocal(newValue);
            }
            else if (clone)
            {
                newValue = (IDictionary<string, object>)new Dictionary<string, object>(newValue);
                MappedDiagnosticsLogicalContext.SetThreadLocal(newValue);
            }
            return newValue;
        }

        /// <summary>
        /// Gets the current logical context named item, as <see cref="T:System.String" />.
        /// </summary>
        /// <param name="item">Item name.</param>
        /// <returns>The value of <paramref name="item" />, if defined; otherwise <see cref="F:System.String.Empty" />.</returns>
        /// <remarks>If the value isn't a <see cref="T:System.String" /> already, this call locks the <see cref="T:NLog.LogFactory" /> for reading the <see cref="P:NLog.Config.LoggingConfiguration.DefaultCultureInfo" /> needed for converting to <see cref="T:System.String" />. </remarks>
        public static string Get(string item)
        {
            return MappedDiagnosticsLogicalContext.Get(item, (IFormatProvider)null);
        }

        /// <summary>
        /// Gets the current logical context named item, as <see cref="T:System.String" />.
        /// </summary>
        /// <param name="item">Item name.</param>
        /// <param name="formatProvider">The <see cref="T:System.IFormatProvider" /> to use when converting a value to a string.</param>
        /// <returns>The value of <paramref name="item" />, if defined; otherwise <see cref="F:System.String.Empty" />.</returns>
        /// <remarks>If <paramref name="formatProvider" /> is <c>null</c> and the value isn't a <see cref="T:System.String" /> already, this call locks the <see cref="T:NLog.LogFactory" /> for reading the <see cref="P:NLog.Config.LoggingConfiguration.DefaultCultureInfo" /> needed for converting to <see cref="T:System.String" />. </remarks>
        public static string Get(string item, IFormatProvider formatProvider)
        {
            return FormatHelper.ConvertToString(MappedDiagnosticsLogicalContext.GetObject(item), formatProvider);
        }

        /// <summary>
        /// Gets the current logical context named item, as <see cref="T:System.Object" />.
        /// </summary>
        /// <param name="item">Item name.</param>
        /// <returns>The value of <paramref name="item" />, if defined; otherwise <c>null</c>.</returns>
        public static object GetObject(string item)
        {
            MappedDiagnosticsLogicalContext.GetLogicalThreadDictionary(false).TryGetValue(item, out var obj);
            return obj;
        }

        /// <summary>
        /// Sets the current logical context item to the specified value.
        /// </summary>
        /// <param name="item">Item name.</param>
        /// <param name="value">Item value.</param>
        /// <returns>&gt;An <see cref="T:System.IDisposable" /> that can be used to remove the item from the current logical context.</returns>
        public static IDisposable SetScoped(string item, string value)
        {
            MappedDiagnosticsLogicalContext.Set(item, value);
            return (IDisposable)new MappedDiagnosticsLogicalContext.ItemRemover(item);
        }

        /// <summary>
        /// Sets the current logical context item to the specified value.
        /// </summary>
        /// <param name="item">Item name.</param>
        /// <param name="value">Item value.</param>
        /// <returns>&gt;An <see cref="T:System.IDisposable" /> that can be used to remove the item from the current logical context.</returns>
        public static IDisposable SetScoped(string item, object value)
        {
            MappedDiagnosticsLogicalContext.Set(item, value);
            return (IDisposable)new MappedDiagnosticsLogicalContext.ItemRemover(item);
        }

        /// <summary>
        /// Sets the current logical context item to the specified value.
        /// </summary>
        /// <param name="item">Item name.</param>
        /// <param name="value">Item value.</param>
        public static void Set(string item, string value)
        {
            MappedDiagnosticsLogicalContext.GetLogicalThreadDictionary(true)[item] = (object)value;
        }

        /// <summary>
        /// Sets the current logical context item to the specified value.
        /// </summary>
        /// <param name="item">Item name.</param>
        /// <param name="value">Item value.</param>
        public static void Set(string item, object value)
        {
            MappedDiagnosticsLogicalContext.GetLogicalThreadDictionary(true)[item] = value;
        }

        /// <summary>Returns all item names</summary>
        /// <returns>A collection of the names of all items in current logical context.</returns>
        public static ICollection<string> GetNames()
        {
            return MappedDiagnosticsLogicalContext.GetLogicalThreadDictionary(false).Keys;
        }

        /// <summary>
        /// Checks whether the specified <paramref name="item" /> exists in current logical context.
        /// </summary>
        /// <param name="item">Item name.</param>
        /// <returns>A boolean indicating whether the specified <paramref name="item" /> exists in current logical context.</returns>
        public static bool Contains(string item)
        {
            return MappedDiagnosticsLogicalContext.GetLogicalThreadDictionary(false).ContainsKey(item);
        }

        /// <summary>
        /// Removes the specified <paramref name="item" /> from current logical context.
        /// </summary>
        /// <param name="item">Item name.</param>
        public static void Remove(string item)
        {
            MappedDiagnosticsLogicalContext.GetLogicalThreadDictionary(true).Remove(item);
        }

        /// <summary>Clears the content of current logical context.</summary>
        public static void Clear()
        {
            MappedDiagnosticsLogicalContext.Clear(false);
        }

        /// <summary>Clears the content of current logical context.</summary>
        /// <param name="free">Free the full slot.</param>
        public static void Clear(bool free)
        {
            if (free)
                MappedDiagnosticsLogicalContext.SetThreadLocal((IDictionary<string, object>)null);
            else
                MappedDiagnosticsLogicalContext.GetLogicalThreadDictionary(true).Clear();
        }

        private static void SetThreadLocal(IDictionary<string, object> newValue)
        {
            MappedDiagnosticsLogicalContext.AsyncLocalDictionary.Value = newValue;
        }

        private static IDictionary<string, object> GetThreadLocal()
        {
            return MappedDiagnosticsLogicalContext.AsyncLocalDictionary.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        private class ItemRemover : IDisposable
        {
            private readonly string _item;
            private int _disposed;

            public ItemRemover(string item)
            {
                this._item = item;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref this._disposed, 1) != 0)
                    return;
                MappedDiagnosticsLogicalContext.Remove(this._item);
            }
        }
    }
}
