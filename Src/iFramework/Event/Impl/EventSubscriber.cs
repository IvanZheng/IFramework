using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using IFramework.Message;
using IFramework.MessageQueue;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.SysExceptions;
using IFramework.Command;
using IFramework.IoC;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.Message.Impl;
using IFramework.Config;
using System.Threading.Tasks;

namespace IFramework.Event.Impl
{
    public class EventSubscriber : IMessageConsumer
    {
        readonly string _topic;
        protected IMessageQueueClient _MessageQueueClient;
        protected ICommandBus _commandBus;
        protected IMessagePublisher _messagePublisher;
        protected IHandlerProvider _handlerProvider;
        protected string _subscriptionName;
        protected int _partition;
        protected MessageProcessor _messageProcessor;
        protected ILogger _logger;
        protected ISlidingDoor _slidingDoor;

        public EventSubscriber(IMessageQueueClient messageQueueClient,
                               IHandlerProvider handlerProvider,
                               ICommandBus commandBus,
                               IMessagePublisher messagePublisher,
                               string subscriptionName,
                               string topic,
                               int partition)
        {
            _MessageQueueClient = messageQueueClient;
            _handlerProvider = handlerProvider;
            _topic = topic;
            _partition = partition;
            _subscriptionName = subscriptionName;
            _messagePublisher = messagePublisher;
            _commandBus = commandBus;
            _messageProcessor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>());
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
        }


        protected void SaveEvent(IMessageContext eventContext)
        {
            using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
            using (var messageStore = scope.Resolve<IMessageStore>())
            {
                messageStore.SaveEvent(eventContext);
            }
        }

        protected async Task ConsumeMessage(IMessageContext eventContext)
        {
            var message = eventContext.Message;
            var messageHandlerTypes = _handlerProvider.GetHandlerTypes(message.GetType());

            if (messageHandlerTypes.Count == 0)
            {
                return;
            }

            SaveEvent(eventContext);
            //messageHandlerTypes.ForEach(messageHandlerType =>
            foreach (var messageHandlerType in messageHandlerTypes)
            {
                using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
                {
                    scope.RegisterInstance(typeof(IMessageContext), eventContext);
                    var messageStore = scope.Resolve<IMessageStore>();
                    var subscriptionName = string.Format("{0}.{1}", _subscriptionName, messageHandlerType.Type.FullName);
                    if (!messageStore.HasEventHandled(eventContext.MessageID, subscriptionName))
                    {
                        var eventMessageStates = new List<MessageState>();
                        var commandMessageStates = new List<MessageState>();
                        var eventBus = scope.Resolve<IEventBus>();
                        try
                        {
                            var messageHandler = scope.Resolve(messageHandlerType.Type);
                            using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                                               new TransactionOptions
                                                                               {
                                                                                   IsolationLevel = IsolationLevel.ReadUncommitted
                                                                               },
                                                                               TransactionScopeAsyncFlowOption.Enabled))
                            {
                                if (messageHandlerType.IsAsync)
                                {
                                    await ((dynamic)messageHandler).Handle((dynamic)message)
                                                                   .ConfigureAwait(false);
                                }
                                else
                                {
                                    await Task.Run(() =>
                                    {
                                        ((dynamic)messageHandler).Handle((dynamic)message);
                                    }).ConfigureAwait(false);
                                }

                                //get commands to be sent
                                eventBus.GetCommands().ForEach(cmd =>
                                   commandMessageStates.Add(new MessageState(_commandBus?.WrapCommand(cmd)))
                               );
                                //get events to be published
                                eventBus.GetEvents().ForEach(msg => eventMessageStates.Add(new MessageState(_MessageQueueClient.WrapMessage(msg, key: msg.Key))));

                                messageStore.HandleEvent(eventContext,
                                                       subscriptionName,
                                                       commandMessageStates.Select(s => s.MessageContext),
                                                       eventMessageStates.Select(s => s.MessageContext));

                                transactionScope.Complete();
                            }
                            if (commandMessageStates.Count > 0)
                            {
                                _commandBus?.SendMessageStates(commandMessageStates);
                            }
                            if (eventMessageStates.Count > 0)
                            {
                                _messagePublisher?.SendAsync(eventMessageStates.ToArray());
                            }
                        }
                        catch (Exception e)
                        {
                            if (e is DomainException)
                            {
                                _logger.Warn(message.ToJson(), e);
                            }
                            else
                            {
                                //IO error or sytem Crash
                                _logger.Error(message.ToJson(), e);
                            }
                            messageStore.Rollback();
                            eventBus.GetToPublishAnywayMessages().ForEach(msg => eventMessageStates.Add(new MessageState(_MessageQueueClient.WrapMessage(msg, key: msg.Key))));
                            messageStore.SaveFailHandledEvent(eventContext, subscriptionName, e, eventMessageStates.Select(s => s.MessageContext).ToArray());
                            if (eventMessageStates.Count > 0)
                            {
                                _messagePublisher?.SendAsync(eventMessageStates.ToArray());
                            }
                        }
                    }
                }
            }
            _slidingDoor.RemoveOffset(eventContext.Offset);
        }
        public void Start()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_topic))
                {
                    var _CommitOffset = _MessageQueueClient.StartSubscriptionClient(_topic, _partition, _subscriptionName, OnMessagesReceived);
                    _slidingDoor = new SlidingDoor(_CommitOffset, 1000, 100, Configuration.Instance.GetCommitPerMessage());
                }
                _messageProcessor.Start();
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException().Message, e);
            }
        }

        public void Stop()
        {
            _MessageQueueClient.Dispose();
            _messageProcessor.Stop();
        }

        protected void OnMessagesReceived(params IMessageContext[] messageContexts)
        {
            messageContexts.ForEach(messageContext =>
            {
                _slidingDoor.AddOffset(messageContext.Offset);
                _messageProcessor.Process(messageContext, ConsumeMessage);
                MessageCount++;
            });
            _slidingDoor.BlockIfFullLoad();
        }

        public string GetStatus()
        {
            return string.Format("Handled message count {0}", MessageCount);
        }

        public decimal MessageCount { get; set; }
    }
}
