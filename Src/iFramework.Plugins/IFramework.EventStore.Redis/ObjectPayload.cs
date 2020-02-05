using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure;

namespace IFramework.EventStore.Redis
{
    public class ObjectPayload
    {
        public ObjectPayload(object obj, string typeCode)
        {
            Code = typeCode ?? obj?.GetType().GetFullNameWithAssembly();
            Payload = obj.ToJson();
        }
        public ObjectPayload()
        {

        }
        public string Code { get; set; }
        public string Payload { get; set; }
    }
}
