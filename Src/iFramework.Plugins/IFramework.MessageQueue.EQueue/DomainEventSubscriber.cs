using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Message;
using System.Threading.Tasks;
using IFramework.SysExceptions;
using IFramework.UnitOfWork;
using IFramework.Message.Impl;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.MessageQueue.EQueue.MessageFormat;
using EQueueClientsConsumers = EQueue.Clients.Consumers;
using EQueueProtocols = EQueue.Protocols;
using IFramework.Event;

namespace IFramework.MessageQueue.EQueue
{
    public class DomainEventSubscriber : MessageConsumer<IFramework.MessageQueue.EQueue.MessageFormat.MessageContext>
    {
        IHandlerProvider HandlerProvider { get; set; }

        public DomainEventSubscriber(string name, EQueueClientsConsumers.ConsumerSetting consumerSetting, 
                                     string groupName, string subscribeTopic,
                                     IHandlerProvider handlerProvider)
            : base(groupName, consumerSetting, groupName, subscribeTopic)
        {
            HandlerProvider = handlerProvider;
        }

        protected override void ConsumeMessage(IFramework.MessageQueue.EQueue.MessageFormat.MessageContext eventContext, EQueueProtocols.QueueMessage queueMessage)
        {

            _Logger.DebugFormat("Start Handle event , messageContextID:{0} queueID:{1}", eventContext.MessageID, queueMessage.QueueId);

            var message = eventContext.Message;
            var messageHandlerTypes = HandlerProvider.GetHandlerTypes(message.GetType());

            if (messageHandlerTypes.Count == 0)
            {
                return;
            }

            messageHandlerTypes.ForEach(messageHandlerType =>
            {
                PerMessageContextLifetimeManager.CurrentMessageContext = eventContext;
                eventContext.ToBeSentMessageContexts.Clear();
                var messageStore = IoCFactory.Resolve<IMessageStore>();
                var subscriptionName = string.Format("{0}.{1}", SubscribeTopic, messageHandlerType.FullName);
                if (!messageStore.HasEventHandled(eventContext.MessageID, subscriptionName))
                {
                    try
                    {
                        var messageHandler = IoCFactory.Resolve(messageHandlerType);
                        ((dynamic)messageHandler).Handle((dynamic)message);
                        var commandContexts = eventContext.ToBeSentMessageContexts;
                        var eventBus = IoCFactory.Resolve<IEventBus>();
                        var messageContexts = new List<MessageContext>();
                        eventBus.GetMessages().ForEach(msg => messageContexts.Add(new MessageContext(msg)));
                       
                        messageStore.SaveEvent(eventContext, subscriptionName, commandContexts, messageContexts);
                        if (commandContexts.Count > 0)
                        {
                            ((CommandBus)IoCFactory.Resolve<ICommandBus>()).SendCommands(commandContexts.AsEnumerable());
                        }
                        if (messageContexts.Count > 0)
                        {
                            IoCFactory.Resolve<IEventPublisher>().Publish(messageContexts.ToArray());
                        }
                    }
                    catch (Exception e)
                    {
                        if (e is DomainException)
                        {
                            _Logger.Warn(message.ToJson(), e);
                        }
                        else
                        {
                            //IO error or sytem Crash
                            _Logger.Error(message.ToJson(), e);
                        }
                        messageStore.SaveFailHandledEvent(eventContext, subscriptionName, e);
                    }
                    finally
                    {
                        PerMessageContextLifetimeManager.CurrentMessageContext = null;
                        MessageCount++;
                    }
                }
            });
        }
    }
}
