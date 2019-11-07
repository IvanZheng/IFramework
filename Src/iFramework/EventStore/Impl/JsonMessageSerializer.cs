using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure;

namespace IFramework.EventStore.Impl
{
    public class JsonEventSerializer: IEventSerializer
    {
        public byte[] Serialize(object data)
        {
            var jsonValue = data.ToJson();
            return Encoding.UTF8.GetBytes(jsonValue);
        }
    }
}
