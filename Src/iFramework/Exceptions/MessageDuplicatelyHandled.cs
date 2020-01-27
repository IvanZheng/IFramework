using System;
using System.Runtime.Serialization;
using IFramework.Event;

namespace IFramework.Exceptions
{
    public class MessageDuplicatelyHandled : Exception
    {
        public string CommandId { get; private set; }
        public string AggregateRootId { get; private set; }
        public IEvent[] Events { get; private set; }
        public object Result { get; private set; }
        public MessageDuplicatelyHandled(string commandId, string aggregateRootId, object result, IEvent[] events = null) 
            : base($"MessageDuplicatelyHandled aggregateRootId:{aggregateRootId} commandId: {commandId}")
        {
            CommandId = commandId;
            AggregateRootId = aggregateRootId;
            Events = events;
            Result = result;
        }

        protected MessageDuplicatelyHandled(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}