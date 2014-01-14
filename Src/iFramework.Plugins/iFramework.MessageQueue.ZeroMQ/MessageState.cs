using IFramework.Message;
using IFramework.Message.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class MessageState
    {
        public MessageState() { }
        public MessageState(IMessageContext messageContext)
        {
            MessageContext = messageContext;
        }
        internal string MessageID { get; set; }
        internal CancellationToken CancellationToken { get; set; }
        internal IMessageContext MessageContext { get; set; }
        internal TaskCompletionSource<object> TaskCompletionSource { get; set; }
    }
}
