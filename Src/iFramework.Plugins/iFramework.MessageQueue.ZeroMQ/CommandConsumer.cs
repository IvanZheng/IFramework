using IFramework.Message;
using IFramework.SysException;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using ZeroMQ;
using IFramework.Message.Impl;
using IFramework.UnitOfWork;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class CommandConsumer : MessageConsumer
    {
        IHandlerProvider HandlerProvider { get; set; }

        public CommandConsumer(IHandlerProvider handlerProvider, string replyToEndPoint, params string[] pullEndPoints)
            : base(replyToEndPoint, pullEndPoints)
        {
            HandlerProvider = handlerProvider;
        }

        protected override void OnMessageHandled(MessageReply reply)
        {
            if (Replier != null)
            {
                Replier.Send(reply.ToJson(), Encoding.UTF8);
            }
            base.OnMessageHandled(reply);
        }

        protected override void Consume(IMessageContext messageContext)
        {
            MessageReply messageReply = null;
            var message = messageContext.Message;
            var messageHandlers = HandlerProvider.GetHandlers(message.GetType());
            try
            {
                if (messageHandlers.Count == 0)
                {
                    messageReply = new MessageReply(messageContext.MessageID, new NoHandlerExists());
                }
                else
                {
                    PerMessageContextLifetimeManager.CurrentMessageContext = messageContext;
                    var unitOfWork = IoCFactory.Resolve<IUnitOfWork>();
                    messageHandlers[0].Handle(message);
                    unitOfWork.Commit();
                    messageReply = new MessageReply(messageContext.MessageID, message.GetValueByKey("Result"));
                }

            }
            catch (Exception e)
            {
                messageReply = new MessageReply(messageContext.MessageID, e.GetBaseException());
                // need log
            }
            finally
            {
                messageContext.ClearItems();
                OnMessageHandled(messageReply);
            }
        }
    }
}
