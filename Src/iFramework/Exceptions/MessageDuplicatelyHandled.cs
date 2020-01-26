using System;
using System.Runtime.Serialization;
using IFramework.Event;

namespace IFramework.Exceptions
{
    public class MessageDuplicatelyHandled : Exception
    {
        public IEvent[] Events { get; private set; }
        public object Result { get; private set; }
        public MessageDuplicatelyHandled(object result, IEvent[] events = null) : base("MessageDuplicatelyHandled")
        {
            Events = events;
            Result = result;
        }

        protected MessageDuplicatelyHandled(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}