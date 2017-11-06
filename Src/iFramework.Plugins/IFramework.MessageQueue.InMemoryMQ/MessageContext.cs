using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue.InMemoryMQ
{
    public class MessageContext : IMessageContext
    {
        public MessageContext(object message, string id = null)
        {
            SentTime = DateTime.Now;
            Message = message;
            if (!string.IsNullOrEmpty(id))
            {
                MessageID = id;
            }
            else if (message is IMessage)
            {
                MessageID = ((IMessage)message).ID;
            }
            else
            {
                MessageID = ObjectId.GenerateNewId().ToString();
            }
            if (message != null && message is IMessage)
            {
                Topic = (message as IMessage).GetTopic();
            }
        }
        public IDictionary<string, object> Headers { get; set; }
        public string Key { get; set; }
        public string MessageID { get; set; }
        public string CorrelationID { get; set; }
        public string ReplyToEndPoint { get; set; }
        public object Reply { get; set; }
        public object Message { get; set; }
        public DateTime SentTime { get; set; }
        public string Topic { get; set; }
        public long Offset { get; set; }
        public SagaInfo SagaInfo { get; set; }
        public string IP { get; set; }
        public string Producer { get; set; }
    }
}
