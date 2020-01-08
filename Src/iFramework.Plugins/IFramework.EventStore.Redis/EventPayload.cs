using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.EventStore.Redis
{
    public class EventPayload
    {
        public string Code { get; set; }
        public string Payload { get; set; }
    }
}
