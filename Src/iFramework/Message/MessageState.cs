using IFramework.Message;
using IFramework.Message.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Message
{
    public class MessageState
    {
        public MessageState() { }
        public MessageState(IMessageContext messageContext)
        {
            MessageContext = messageContext;
        }
        public string MessageID { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public IMessageContext MessageContext { get; set; }
        public TaskCompletionSource<object> TaskCompletionSource { get; set; }
    }
}
