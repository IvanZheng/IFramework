using System;
using System.Collections.Generic;
using System.Text;

namespace IFramework.EventStore.Redis
{
    public class MessageAttachment
    {
        public ObjectPayload CommandResult { get; set; }
        public ObjectPayload SagaResult { get; set; }
        public long Version { get; set; }

        public MessageAttachment(){}
        public MessageAttachment(ObjectPayload commandResult, ObjectPayload sagaResult, long version)
        {
            CommandResult = commandResult;
            SagaResult = sagaResult;
            Version = version;
        }
    }
}
