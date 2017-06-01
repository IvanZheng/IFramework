using System;
using System.Collections.Generic;

namespace IFramework.Message.Impl
{
    public class EmptyMessageContext : IMessageContext
    {
        public EmptyMessageContext() { }

        public EmptyMessageContext(IMessage message)
        {
            SentTime = DateTime.Now;
            Message = message;
            MessageID = message.ID;
        }

        public string FromEndPoint { get; set; }


        public List<IMessageContext> ToBeSentMessageContexts => null;

        public long Offset { get; set; }

        public IDictionary<string, object> Headers => null;

        public string Key => null;

        public SagaInfo SagaInfo => null;

        public string MessageID { get; set; }

        public string ReplyToEndPoint => null;

        public object Reply { get; set; }

        public object Message { get; set; }

        public DateTime SentTime { get; set; }


        public string CorrelationID { get; set; }


        public string Topic { get; set; }

        public string IP { get; set; }

        public string Producer { get; set; }
    }
}