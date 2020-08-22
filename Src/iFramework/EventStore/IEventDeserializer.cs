using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.EventStore
{
    public interface IEventDeserializer
    {
        /// <summary>Deserialize a message key or value.</summary>
        /// <param name="data">The data to deserialize.</param>
        /// <returns>The deserialized value.</returns>
        T Deserialize<T>(ReadOnlySpan<byte> data);

        object Deserialize(ReadOnlySpan<byte> data, Type type);
    }
}
