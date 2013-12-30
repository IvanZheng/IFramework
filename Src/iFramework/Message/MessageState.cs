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
    class MessageState
    {
        public MessageState() { }
        public MessageState(object message)
        {
            Message= message;
        }
        internal string MessageID { get; set; }
        internal CancellationToken CancellationToken{get;set;}
        internal object Message{get;set;}
        internal TaskCompletionSource<object> TaskCompletionSource{get;set;}
    }
}
