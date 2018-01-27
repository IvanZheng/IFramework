using System;
using System.Collections.Generic;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.Tests
{
    public class MessageContext : IMessageContext
    {
        public List<IMessageContext> ToBeSentMessageContexts { get; set; }

        public string CorrelationID { get; set; }

        public string Producer { get; set; }
        public string IP { get; set; }

        public long Offset { get; set; }

        public IDictionary<string, object> Headers { get; set; }

        public string Key { get; set; }

        public object Message { get; set; }

        public string MessageID { get; set; }

        public object Reply { get; set; }

        public string ReplyToEndPoint { get; set; }

        public DateTime SentTime { get; set; }

        public string Topic { get; set; }

        public SagaInfo SagaInfo { get; set; }
    }
}