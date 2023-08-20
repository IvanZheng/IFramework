using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                                            ConsumerConfig consumerConfig = null,
                                            IMessageContextBuilder messageContextBuilder = null)
            : base(messageQueueClient,
                   handlerProvider,
                   commandBus,
                   messagePublisher,
                   subscriptionName,
                   topicSubscriptions,
                   consumerId,
                   consumerConfig,
                   messageContextBuilder)
        {
            _eventStore = eventStore;
        }

        protected override async Task ConsumeMessage(IMessageContext eventContext, CancellationToken cancellationToken)
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
                            await ((dynamic) messageHandler).Handle((dynamic) message, cancellationToken)
                                                            .ConfigureAwait(false);
                        }
                        else
                        {
                            ((dynamic) messageHandler).Handle((dynamic) message);
                        }
                    }
                    catch (Exception e)
                    {
                        eventBus.ClearMessages();
                        if (e is DomainException exception)
                        {
                            eventContext.Reply = e;
                            var domainExceptionEvent = exception.DomainExceptionEvent;
                            if (domainExceptionEvent != null)
                            {
                                eventBus.Publish(domainExceptionEvent);
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
                            eventContext.Reply = new Exception(e.GetBaseException().Message);
                            Logger.LogError(e, message.ToJson());
                        }
                    }
                    finally
                    {
                        var result = await _eventStore.HandleEvent(subscriptionName,
                                                                   eventContext.MessageId,
                                                                   eventBus.GetCommands().ToArray(),
                                                                   eventBus.GetEvents().ToArray(),
                                                                   eventBus.GetSagaResult(),
                                                                   eventContext.Reply);
                        //get commands to be sent
                        result.commands
                              .ForEach(cmd =>
                                           commandMessageStates.Add(new MessageState(CommandBus?.WrapCommand(cmd,
                                                                                                             sagaInfo: sagaInfo,
                                                                                                             producer: Producer)))
                                      );
                        //get events to be published
                        result.events
                              .ForEach(msg =>
                              {
                                  var topic = msg.GetFormatTopic();
                                  eventMessageStates.Add(new MessageState(MessageQueueClient.WrapMessage(msg,
                                                                                                         topic: topic,
                                                                                                         key: msg.Key,
                                                                                                         sagaInfo: sagaInfo,
                                                                                                         producer: Producer)));
                              });

                        if (sagaInfo != null && !string.IsNullOrWhiteSpace(sagaInfo.SagaId) && result.sagaResult != null)
                        {
                            var topic = sagaInfo.ReplyEndPoint;
                            if (!string.IsNullOrEmpty(topic))
                            {
                                var sagaReply = MessageQueueClient.WrapMessage(result.sagaResult,
                                                                               topic: topic,
                                                                               messageId: ObjectId.GenerateNewId().ToString(),
                                                                               sagaInfo: sagaInfo,
                                                                               producer: Producer);
                                eventMessageStates.Add(new MessageState(sagaReply));
                            }
                        }

                        if (commandMessageStates.Count > 0)
                        {
                            CommandBus?.SendMessageStates(commandMessageStates);
                        }

                        if (eventMessageStates.Count > 0)
                        {
                            MessagePublisher?.SendAsync(CancellationToken.None,
                                                        eventMessageStates.ToArray());
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