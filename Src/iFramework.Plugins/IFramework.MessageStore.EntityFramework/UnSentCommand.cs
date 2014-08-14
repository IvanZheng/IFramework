using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.MessageStoring
{
    public class UnSentCommand : UnSentMessage
    {
         public UnSentCommand() { }
         public UnSentCommand(IMessageContext messageContext) :
            base(messageContext)
        {
        }
    }
}
