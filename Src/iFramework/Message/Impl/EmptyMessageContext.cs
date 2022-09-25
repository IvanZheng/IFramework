using System;
using System.Collections.Generic;
using IFramework.MessageQueue;

namespace IFramework.Message.Impl
{
    public class EmptyMessageContext : IMessageContext
    {
        public EmptyMessageContext()
        {
            MessageOffset = new MessageOffset();
        }

        public EmptyMessageContext(IMessage message)
        {
            SentTime = DateTime.Now;
            Message = message;
            MessageId = message.Id;
            MessageOffset = new MessageOffset();
        }

        public string FromEndPoint { get; set; }


        public List<IMessageContext> ToBeSentMessageContexts => null;

        public IDictionary<string, object> Headers => null;

        public string Key { get; set; }
        public string[] Tags => null;

        public SagaInfo SagaInfo => null;

        public string MessageId { get; set; }

        public string ReplyToEndPoint => null;

        public object Reply { get; set; }

        public object Message { get; set; }

        public DateTime SentTime { get; set; }


        public string CorrelationId { get; set; }


        public string Topic { get; set; }

        public string Ip { get; set; }

        public string Producer { get; set; }
        public MessageOffset MessageOffset { get; }

        public string MessageType { get; set; }
    }
}