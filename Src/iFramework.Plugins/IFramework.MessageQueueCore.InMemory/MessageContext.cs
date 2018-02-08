using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueueCore.InMemory
{
    public class MessageContext : IMessageContext
    {
        public MessageContext(object message, string id = null)
        {
            SentTime = DateTime.Now;
            Message = message;
            if (!string.IsNullOrEmpty(id))
            {
                MessageId = id;
            }
            else if (message is IMessage)
            {
                MessageId = ((IMessage)message).Id;
            }
            else
            {
                MessageId = ObjectId.GenerateNewId().ToString();
            }
            if (message is IMessage iMessage)
            {
                Topic = iMessage.GetTopic();
            }
        }
        public IDictionary<string, object> Headers { get; set; }
        public string Key { get; set; }
        public string MessageId { get; set; }
        public string CorrelationId { get; set; }
        public string ReplyToEndPoint { get; set; }
        public object Reply { get; set; }
        public object Message { get; set; }
        public DateTime SentTime { get; set; }
        public string Topic { get; set; }
        public long Offset { get; set; }
        public SagaInfo SagaInfo { get; set; }
        public string Ip { get; set; }
        public string Producer { get; set; }
    }
}
