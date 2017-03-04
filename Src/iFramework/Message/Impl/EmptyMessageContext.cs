using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Message.Impl
{
    public class EmptyMessageContext : IMessageContext
    {
        public EmptyMessageContext()
        {

        }
        public EmptyMessageContext(IMessage message)
        {
            SentTime = DateTime.Now;
            Message = message;
            MessageID = message.ID;
        }

        public long Offset { get; set; }

        public IDictionary<string, object> Headers
        {
            get { return null; }
        }

        public string Key
        {
            get { return null; }
        }

        public SagaInfo SagaInfo
        {
            get { return null; }
        }

        public string MessageID
        {
            get;
            set;
        }

        public string ReplyToEndPoint
        {
            get { return null; }
        }

        public object Reply
        {
            get;
            set;
        }

        public string FromEndPoint
        {
            get;
            set;
        }

        public object Message
        {
            get;
            set;
        }

        public DateTime SentTime
        {
            get;
            set;
        }


        public string CorrelationID
        {
            get;
            set;
        }


        public string Topic
        {
            get;
            set;
        }


        public List<IMessageContext> ToBeSentMessageContexts
        {
            get { return null; }
        }

        public string IP
        {
            get;set;
        }

        public string Producer
        {
            get;set;
        }
    }
}
