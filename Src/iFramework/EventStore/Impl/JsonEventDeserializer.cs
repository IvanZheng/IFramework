using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure;

namespace IFramework.EventStore.Impl
{
    public class JsonEventDeserializer: IEventDeserializer
    {
        public T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            return Encoding.UTF8.GetString(data.ToArray()).ToJsonObject<T>(true);
        }

        public object Deserialize(ReadOnlySpan<byte> data, Type type)
        {
            return Encoding.UTF8.GetString(data.ToArray()).ToJsonObject(type, true);
        }
    }
}
