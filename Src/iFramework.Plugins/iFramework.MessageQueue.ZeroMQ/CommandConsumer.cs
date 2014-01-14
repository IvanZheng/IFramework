using IFramework.Infrastructure;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.MessageFormat;
using IFramework.SysException;
using IFramework.UnitOfWork;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQ;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class CommandConsumer : MessageConsumer<IMessageContext>
    {
        protected IHandlerProvider HandlerProvider { get; set; }

        public CommandConsumer(IHandlerProvider handlerProvider, string receiveEndPoint)
            : base(receiveEndPoint)
        {
            HandlerProvider = handlerProvider;
        }

        void OnMessageHandled(IMessageContext messageContext, IMessageReply reply)
        {
            if (!string.IsNullOrWhiteSpace(messageContext.ReplyToEndPoint))
            {
                var replySender = GetReplySender(messageContext.ReplyToEndPoint);
                if (replySender != null)
                {
                    replySender.SendFrame(reply.GetFrame());
                }
            }

            if (!string.IsNullOrWhiteSpace(messageContext.FromEndPoint))
            {
                var notificationSender = GetReplySender(messageContext.FromEndPoint);
                if (notificationSender != null)
                {
                    notificationSender.SendFrame(new MessageHandledNotification(messageContext.MessageID)
                                                        .GetFrame());
                }
            }
        }

        protected override void ConsumeMessage(IMessageContext messageContext)
        {
            IMessageReply messageReply = null;
            if (messageContext == null || messageContext.Message == null)
            {
                return;
            }
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
                OnMessageHandled(messageContext, messageReply);
            }
        }

        protected override void ReceiveMessage(Frame frame)
        {
            var messageContext = frame.GetMessage<MessageContext>();
            if (messageContext != null)
            {
                MessageQueue.Add(messageContext);
            }
        }
    }
}
