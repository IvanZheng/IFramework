using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message.Impl
{
    class MessageHandler<TMessage> : IMessageHandler where TMessage : class
    {
        readonly IMessageHandler<TMessage> _messageHandler;

        public MessageHandler(IMessageHandler<TMessage> messageHandler)
        {
            _messageHandler = messageHandler;
        }

        public void Handle(object message)
        {
            _messageHandler.Handle(message as TMessage);
        }
    }

}
