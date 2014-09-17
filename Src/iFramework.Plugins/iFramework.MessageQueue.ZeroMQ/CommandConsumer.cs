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

        protected override void ConsumeMessage(IMessageContext commandContext)
        {
            var message = commandContext.Message as ICommand;
            MessageReply messageReply = null;
            if (message == null)
            {
                return;
            }
            var needRetry = message.NeedRetry;
            bool commandHasHandled = false;
            IMessageStore messageStore = null;
            try
            {
                PerMessageContextLifetimeManager.CurrentMessageContext = commandContext;
                messageStore = IoCFactory.Resolve<IMessageStore>();
                commandHasHandled = messageStore.HasCommandHandled(commandContext.MessageID);
                if (!commandHasHandled)
                {
                    var messageHandler = HandlerProvider.GetHandler(message.GetType());
                    _Logger.InfoFormat("Handle command, commandID:{0}", commandContext.MessageID);

                    if (messageHandler == null)
                    {
                        messageReply = new MessageReply(commandContext.MessageID, new NoHandlerExists());
                    }
                    else
                    {
                        //var unitOfWork = IoCFactory.Resolve<IUnitOfWork>();
                        do
                        {
                            try
                            {
                                ((dynamic)messageHandler).Handle((dynamic)message);
                                //unitOfWork.Commit();
                                messageReply = new MessageReply(commandContext.MessageID, commandContext.Reply);
                                needRetry = false;
                            }
                            catch (Exception ex)
                            {
                                if (!(ex is OptimisticConcurrencyException) || !needRetry)
                                {
                                    throw;
                                }
                            }
                        } while (needRetry);
                    }
                }
                else
                {
                    messageReply = new MessageReply(commandContext.MessageID, new MessageDuplicatelyHandled());
                }
            }
            catch (Exception e)
            {
                messageReply = new MessageReply(commandContext.MessageID, e.GetBaseException());
                if (e is DomainException)
                {
                    _Logger.Warn(message.ToJson(), e);
                }
                else
                {
                    _Logger.Error(message.ToJson(), e);
                }
                if (messageStore != null)
                {
                    messageStore.SaveFailedCommand(commandContext);
                }
            }
            finally
            {
                PerMessageContextLifetimeManager.CurrentMessageContext = null;
            }
            if (!commandHasHandled)
            {
                OnMessageHandled(commandContext, messageReply);
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
