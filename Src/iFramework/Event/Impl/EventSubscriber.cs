using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Command;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.DependencyInjection;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using Microsoft.Extensions.Logging;

namespace IFramework.Event.Impl
{
    public class EventSubscriber: IMessageConsumer
    {
        private readonly string _topic;
        protected ICommandBus CommandBus;
        protected string ConsumerId;
        protected IHandlerProvider HandlerProvider;
        protected ICommitOffsetable InternalConsumer;
        protected ILogger Logger;
        protected MessageProcessor MessageProcessor;
        protected IMessagePublisher MessagePublisher;
        protected IMessageQueueClient MessageQueueClient;
        protected ConsumerConfig ConsumerConfig;
        private string _producer;
        protected string SubscriptionName;

        public EventSubscriber(IMessageQueueClient messageQueueClient,
                               IHandlerProvider handlerProvider,
                               ICommandBus commandBus,
                               IMessagePublisher messagePublisher,
                               string subscriptionName,
                               string topic,
                               string consumerId,
                               ConsumerConfig consumerConfig = null)
        {
            ConsumerConfig = consumerConfig ?? ConsumerConfig.DefaultConfig;
            MessageQueueClient = messageQueueClient;
            HandlerProvider = handlerProvider;
            _topic = topic;
            ConsumerId = consumerId;
            SubscriptionName = subscriptionName;
            MessagePublisher = messagePublisher;
            CommandBus = commandBus;
            MessageProcessor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>(),
                                                     ConsumerConfig.MailboxProcessBatchCount);
            Logger = IoCFactory.GetService<ILoggerFactory>().CreateLogger(GetType().Name);
        }

        public string Producer => _producer ?? (_producer = $"{SubscriptionName}.{_topic}.{ConsumerId}");


        public void Start()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_topic))
                {
                    InternalConsumer =
                        MessageQueueClient.StartSubscriptionClient(_topic, SubscriptionName, ConsumerId,
                                                                    OnMessagesReceived, 
                                                                    ConsumerConfig);
                }
                MessageProcessor.Start();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Event Subscriber {_topic} start faield");
            }
        }

        public void Stop()
        {
            InternalConsumer.Stop();
            MessageProcessor.Stop();
        }

        public string GetStatus()
        {
            return string.Format("Handled message count {0}", MessageCount);
        }

        public decimal MessageCount { get; set; }

        protected async Task ConsumeMessage(IMessageContext eventContext)
        {
            try
            {
                Logger.LogDebug($"start handle event {ConsumerId} {eventContext.Message.ToJson()}");

                var message = eventContext.Message;
                var sagaInfo = eventContext.SagaInfo;
                var messageHandlerTypes = HandlerProvider.GetHandlerTypes(message.GetType());

                if (messageHandlerTypes.Count == 0)
                {
                    Logger.LogDebug($"event has no handlerTypes, messageType:{message.GetType()} message:{message.ToJson()}");
                    InternalConsumer.CommitOffset(eventContext);
                    return;
                }
                
                //messageHandlerTypes.ForEach(messageHandlerType =>
                foreach (var messageHandlerType in messageHandlerTypes)
                {
                    using (var scope = IoCFactory.Instance
                                                 .ObjectProvider
                                                 .CreateScope(builder => builder.RegisterInstance(typeof(IMessageContext), eventContext)))
                    {
                        var messageStore = scope.GetService<IMessageStore>();
                        var subscriptionName = $"{SubscriptionName}.{messageHandlerType.Type.FullName}";
                        if (!messageStore.HasEventHandled(eventContext.MessageId, subscriptionName))
                        {
                            var eventMessageStates = new List<MessageState>();
                            var commandMessageStates = new List<MessageState>();
                            var eventBus = scope.GetService<IEventBus>();
                            try
                            {
                                var messageHandler = scope.GetRequiredService(messageHandlerType.Type);
                                using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                                                   new TransactionOptions
                                                                                   {
                                                                                       IsolationLevel = IsolationLevel.ReadCommitted
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
                                        await Task.Run(() => { ((dynamic)messageHandler).Handle((dynamic)message); })
                                                  .ConfigureAwait(false);
                                    }

                                    //get commands to be sent
                                    eventBus.GetCommands()
                                            .ForEach(cmd =>
                                                         commandMessageStates.Add(new MessageState(CommandBus?.WrapCommand(cmd,
                                                                                                                            sagaInfo: sagaInfo, producer: Producer)))
                                                    );
                                    //get events to be published
                                    eventBus.GetEvents()
                                            .ForEach(msg =>
                                            {
                                                var topic = msg.GetFormatTopic();
                                                eventMessageStates.Add(new MessageState(MessageQueueClient.WrapMessage(msg,
                                                                                                                        topic: topic,
                                                                                                                        key: msg.Key, sagaInfo: sagaInfo, producer: Producer)));
                                            });

                                    eventBus.GetToPublishAnywayMessages()
                                            .ForEach(msg =>
                                            {
                                                var topic = msg.GetFormatTopic();
                                                eventMessageStates.Add(new MessageState(MessageQueueClient.WrapMessage(msg,
                                                                                                                        topic: topic, key: msg.Key,
                                                                                                                        sagaInfo: sagaInfo, producer: Producer)));
                                            });

                                    eventMessageStates.AddRange(GetSagaReplyMessageStates(sagaInfo, eventBus));

                                    messageStore.HandleEvent(eventContext,
                                                             subscriptionName,
                                                             commandMessageStates.Select(s => s.MessageContext),
                                                             eventMessageStates.Select(s => s.MessageContext));

                                    transactionScope.Complete();
                                }
                                if (commandMessageStates.Count > 0)
                                {
                                    CommandBus?.SendMessageStates(commandMessageStates);
                                }
                                if (eventMessageStates.Count > 0)
                                {
                                    MessagePublisher?.SendAsync(CancellationToken.None, eventMessageStates.ToArray());
                                }
                            }
                            catch (Exception e)
                            {
                                eventMessageStates.Clear();
                                messageStore.Rollback();
                                if (e is DomainException)
                                {
                                    var domainExceptionEvent = ((DomainException)e).DomainExceptionEvent;
                                    if (domainExceptionEvent != null)
                                    {
                                        var topic = domainExceptionEvent.GetFormatTopic();
                                        var exceptionMessage = MessageQueueClient.WrapMessage(domainExceptionEvent,
                                                                                               eventContext.MessageId,
                                                                                               topic,
                                                                                               producer: Producer);
                                        eventMessageStates.Add(new MessageState(exceptionMessage));
                                    }
                                    Logger.LogWarning(e, message.ToJson());
                                }
                                else
                                {
                                    //IO error or sytem Crash
                                    //if we meet with unknown exception, we interrupt saga
                                    if (sagaInfo != null)
                                    {
                                        eventBus.FinishSaga(e);
                                    }
                                    Logger.LogError(e, message.ToJson());
                                }

                                eventBus.GetToPublishAnywayMessages()
                                        .ForEach(msg =>
                                        {
                                            var topic = msg.GetFormatTopic();
                                            eventMessageStates.Add(new MessageState(MessageQueueClient.WrapMessage(msg,
                                                                                                                    topic: topic, key: msg.Key, sagaInfo: sagaInfo, producer: Producer)));
                                        });

                                eventMessageStates.AddRange(GetSagaReplyMessageStates(sagaInfo, eventBus));

                                messageStore.SaveFailHandledEvent(eventContext, subscriptionName, e,
                                                                  eventMessageStates.Select(s => s.MessageContext).ToArray());
                                if (eventMessageStates.Count > 0)
                                {
                                    MessagePublisher?.SendAsync(CancellationToken.None, eventMessageStates.ToArray());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, $"Handle event failed event: {eventContext.ToJson()}");
            }
            InternalConsumer.CommitOffset(eventContext);
        }


        private List<MessageState> GetSagaReplyMessageStates(SagaInfo sagaInfo, IEventBus eventBus)
        {
            var eventMessageStates = new List<MessageState>();
            if (sagaInfo != null && !string.IsNullOrWhiteSpace(sagaInfo.SagaId))
            {
                eventBus.GetSagaResults()
                        .ForEach(sagaResult =>
                        {
                            var topic = sagaInfo.ReplyEndPoint;
                            if (!string.IsNullOrEmpty(topic))
                            {
                                var sagaReply = MessageQueueClient.WrapMessage(sagaResult,
                                                                                topic: topic,
                                                                                messageId: ObjectId.GenerateNewId().ToString(),
                                                                                sagaInfo: sagaInfo,
                                                                                producer: Producer);
                                eventMessageStates.Add(new MessageState(sagaReply));
                            }
                        });
            }
            return eventMessageStates;
        }

        protected void OnMessagesReceived(params IMessageContext[] messageContexts)
        {
            messageContexts.ForEach(messageContext =>
            {
                MessageProcessor.Process(messageContext, ConsumeMessage);
                MessageCount++;
            });
        }
    }
}