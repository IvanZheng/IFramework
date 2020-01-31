namespace IFramework.EventStore.Redis
{
    public class CommandResult
    {
        public CommandResult() { }

        public CommandResult(ObjectPayload result,
                             ObjectPayload sagaResult,
                             ObjectPayload[] applicationEventPayloads = null,
                             ObjectPayload[] aggregateRootEventPayloads = null)
        {
            Result = result;
            SagaResult = sagaResult;
            AggregateRootEventPayloads = aggregateRootEventPayloads;
            ApplicationEventPayloads = applicationEventPayloads;
        }

        public ObjectPayload Result { get; set; }
        public ObjectPayload SagaResult { get; set; }
        public ObjectPayload[] AggregateRootEventPayloads { get; set; }
        public ObjectPayload[] ApplicationEventPayloads { get; set; }
    }
}