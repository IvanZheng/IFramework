using System;
using System.Runtime.Serialization;
using IFramework.Event;

namespace IFramework.Exceptions
{
    public class MessageDuplicatelyHandled : Exception
    {
        public string CommandId { get; private set; }
        public string AggregateRootId { get; private set; }
        public IEvent[] AggregateRootEvents { get; private set; }
        public IEvent[] ApplicationEvents { get; private set; }
        public object CommandResult { get; private set; }
        public object SagaResult { get; private set; }
        public MessageDuplicatelyHandled(string commandId, 
                                         string aggregateRootId, 
                                         object commandResult, 
                                         object sagaResult,
                                         IEvent[] aggregateRootEvents,
                                         IEvent[] applicationEvents = null) 
            : base($"MessageDuplicatelyHandled aggregateRootId:{aggregateRootId} commandId: {commandId}")
        {
            CommandId = commandId;
            AggregateRootId = aggregateRootId;
            AggregateRootEvents = aggregateRootEvents;
            ApplicationEvents = applicationEvents;
            CommandResult = commandResult;
            SagaResult = sagaResult;
        }

        protected MessageDuplicatelyHandled(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}