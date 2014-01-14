using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message.Impl
{
    public class MessageHandledNotification : IMessageHandledNotification
    {
        public string MessageID { get; set; }

        public MessageHandledNotification(string messageID)
        {
            MessageID = messageID;
        }
    }
}
