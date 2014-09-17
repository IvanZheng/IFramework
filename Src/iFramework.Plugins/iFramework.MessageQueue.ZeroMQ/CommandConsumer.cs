using IFramework.Command;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Message;
using IFramework.MessageQueue.ZeroMQ.MessageFormat;
using IFramework.SysExceptions;
using IFramework.UnitOfWork;
using System;
using System.Data;
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
                    _Logger.InfoFormat("send reply, commandID:{0}", reply.MessageID);
                }
            }

            if (!string.IsNullOrWhiteSpace(messageContext.FromEndPoint))
            {
                var notificationSender = GetReplySender(messageContext.FromEndPoint);
                if (notificationSender != null)
                {
                    notificationSender.SendFrame(new MessageHandledNotification(messageContext.MessageID)
                                                        .GetFrame());
                    _Logger.InfoFormat("send notification, commandID:{0}", messageContext.MessageID);

                }
            }
        }

        protected override void ConsumeMessage(IMessageContext messageContext)
        {
            IMessageReply messageReply = null;
            if (messageContext == null || messageContext.Message as ICommand == null)
            {
                return;
            }
            var message = messageContext.Message as ICommand;
            var needRetry = message.NeedRetry;

            do
            {
                try
                {
                    PerMessageContextLifetimeManager.CurrentMessageContext = messageContext;
                    var messageHandler = HandlerProvider.GetHandler(message.GetType());
                    _Logger.InfoFormat("Handle command, commandID:{0}", messageContext.MessageID);

                    if (messageHandler == null)
                    {
                        messageReply = new MessageReply(messageContext.MessageID, new NoHandlerExists());
                    }
                    else
                    {
                        ((dynamic)messageHandler).Handle((dynamic)message);
                        messageReply = new MessageReply(messageContext.MessageID, messageContext.Reply);
                    }
                    needRetry = false;
                }
                catch (Exception e)
                {
                    if (!(e is OptimisticConcurrencyException) || !needRetry)
                    {
                        messageReply = new MessageReply(messageContext.MessageID, e.GetBaseException());
                        if (e is DomainException)
                        {
                            _Logger.Warn(message.ToJson(), e);
                        }
                        else
                        {
                            _Logger.Error(message.ToJson(), e);
                        }
                        needRetry = false;
                    }
                }
                finally
                {
                    PerMessageContextLifetimeManager.CurrentMessageContext = null;
                }
            } while (needRetry);
            OnMessageHandled(messageContext, messageReply);
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
