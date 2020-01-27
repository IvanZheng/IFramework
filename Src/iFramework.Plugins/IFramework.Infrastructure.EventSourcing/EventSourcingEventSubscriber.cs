using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Command;
using IFramework.DependencyInjection;
using IFramework.Event;
using IFramework.Event.Impl;
using IFramework.Exceptions;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using Microsoft.Extensions.Logging;

namespace IFramework.Infrastructure.EventSourcing
{
    public class EventSourcingEventSubscriber : EventSubscriber
    {
        private readonly IEventStore _eventStore;

        public EventSourcingEventSubscriber(IMessageQueueClient messageQueueClient,
                                            IHandlerProvider handlerProvider,
                                            ICommandBus commandBus,
                                            IMessagePublisher messagePublisher,
                                            string subscriptionName,
                                            TopicSubscription[] topicSubscriptions,
                                            string consumerId,
                                            IEventStore eventStore,
                                            ConsumerConfig consumerConfig = null)
            : base(messageQueueClient,
                   handlerProvider, 
                   commandBus, 
                   messagePublisher, 
                   subscriptionName,
                   topicSubscriptions,
                   consumerId,
                   consumerConfig)
        {
            _eventStore = eventStore;
        }

        protected override async Task ConsumeMessage(IMessageContext eventContext)
        {
            try
            {
                Logger.LogDebug($"start handle event {ConsumerId} {eventContext.Message.ToJson()}");

                var message = eventContext.Message;
                if (message == null)
                {
                    Logger.LogDebug($"message is null! messageContext: {eventContext.ToJson()}");
                    return;
                }
                var sagaInfo = eventContext.SagaInfo;
                var messageHandlerTypes = HandlerProvider.GetHandlerTypes(message.GetType());

                if (messageHandlerTypes.Count == 0)
                {
                    Logger.LogDebug($"event has no handlerTypes, messageType:{message.GetType()} message:{message.ToJson()}");
                    return;
                }

                //messageHandlerTypes.ForEach(messageHandlerType =>
                foreach (var messageHandlerType in messageHandlerTypes)
                {
                    using var scope = ObjectProviderFactory.Instance
                                                           .ObjectProvider
                                                           .CreateScope(builder => builder.RegisterInstance(typeof(IMessageContext), eventContext));
                    var messageStore = scope.GetService<IMessageStore>();
                    var subscriptionName = $"{SubscriptionName}.{messageHandlerType.Type.FullName}";
                    using var _ = Logger.BeginScope(new
                    {
                        eventContext.Topic,
                        eventContext.MessageId,
                        eventContext.Key,
                        subscriptionName
                    });
                    var eventMessageStates = new List<MessageState>();
                    var commandMessageStates = new List<MessageState>();
                    var eventBus = scope.GetService<IEventBus>();
                    try
                    {
                        
                            var messageHandler = scope.GetRequiredService(messageHandlerType.Type);
                            
                                if (messageHandlerType.IsAsync)
                                {
                                    await ((dynamic) messageHandler).Handle((dynamic) message)
                                                                    .ConfigureAwait(false);
                                }
                                else
                                {
                                    ((dynamic) messageHandler).Handle((dynamic) message);
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



                                _eventStore.HandleEvent(subscriptionName, eventContext.MessageId, null, null);


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
                        if (e is DomainException exception)
                        {
                            var domainExceptionEvent = exception.DomainExceptionEvent;
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
                                                                                                           topic: topic,
                                                                                                           key: msg.Key,
                                                                                                           sagaInfo: sagaInfo,
                                                                                                           producer: Producer)));
                                });

                       

                        await messageStore.SaveFailHandledEventAsync(eventContext, subscriptionName, e,
                                                                     eventMessageStates.Select(s => s.MessageContext).ToArray())
                                          .ConfigureAwait(false);
                        if (eventMessageStates.Count > 0)
                        {
                            var sendTask = MessagePublisher.SendAsync(CancellationToken.None, eventMessageStates.ToArray());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, $"Handle event failed event: {eventContext.ToJson()}");
            }
            finally
            {
                InternalConsumer.CommitOffset(eventContext);
            }
        }
    }
}