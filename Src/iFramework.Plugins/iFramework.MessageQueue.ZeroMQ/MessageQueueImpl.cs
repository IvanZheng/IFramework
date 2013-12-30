using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Command;
using ZeroMQ;
using System.Threading.Tasks;
using IFramework.Message.Impl;
using System.Collections;
using System.Runtime.Remoting.Messaging;

namespace IFramework.MessageQueue.ZeroMQ
{
    
    public static class ZeroMessageQueue
    {
        static ZmqContext _ZmqContext;
        public static ZmqContext ZmqContext
        {
            get
            {
                return _ZmqContext ?? (_ZmqContext = ZmqContext.Create());
            }
        }
    }
}
