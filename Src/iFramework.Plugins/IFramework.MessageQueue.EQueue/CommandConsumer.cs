using EQueue.Clients.Consumers;
using EQueue.Clients.Producers;
using EQueue.Protocols;
using IFramework.Message;
using IFramework.MessageQueue.MessageFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.UnitOfWork;
using IFramework.SysException;

namespace IFramework.MessageQueue.EQueue
{
    public class CommandConsumer : MessageConsumer<MessageContext>
    {
        protected IHandlerProvider HandlerProvider { get; set; }
        protected Producer Producer { get; set; }

        public CommandConsumer(string name, ConsumerSettings consumerSettings, string groupName,
                               string subscribeTopic,  string brokerAddress, int producerBrokerPort,
                               IHandlerProvider handlerProvider)
            : base(name, consumerSettings, groupName, MessageModel.Clustering, subscribeTopic)
        {
            HandlerProvider = handlerProvider;
            Producer = new Producer(brokerAddress, producerBrokerPort);
        }


        void OnMessageHandled(IMessageContext messageContext, IMessageReply reply)
        {
            if (!string.IsNullOrWhiteSpace(messageContext.ReplyToEndPoint))
            {
                var messageBody = System.Text.Encoding.UTF8.GetBytes(reply.ToJson());
                Producer.Send(new global::EQueue.Protocols.Message(messageContext.ReplyToEndPoint, messageBody), string.Empty);
                _Logger.DebugFormat("send reply, commandID:{0}", reply.MessageID);
            }
        }

        protected override void ConsumeMessage(MessageContext messageContext)
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
                _Logger.DebugFormat("Handle command, commandID:{0}", messageContext.MessageID);

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
    }
}
