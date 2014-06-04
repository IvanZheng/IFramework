using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.EntityFramework.MessageStoring
{
    public class UnPublishedEvent : UnSentMessage
    {
        public UnPublishedEvent() { }
        public UnPublishedEvent(IMessageContext messageContext) :
            base(messageContext)
        {
        }
    }
}
