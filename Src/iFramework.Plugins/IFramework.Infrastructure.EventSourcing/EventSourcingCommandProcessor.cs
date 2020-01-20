using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Command;
using IFramework.Command.Impl;
using IFramework.DependencyInjection;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using Microsoft.Extensions.Logging;

namespace IFramework.Infrastructure.EventSourcing
{
    public class EventSourcingCommandProcessor : CommandProcessor
    {
        private readonly IEventStore _eventStore;

        public EventSourcingCommandProcessor(IMessageQueueClient messageQueueClient,
                                             IMessagePublisher messagePublisher,
                                             IHandlerProvider handlerProvider,
                                             IEventStore eventStore,
                                             string commandQueueName,
                                             string consumerId,
                                             ConsumerConfig consumerConfig = null)
            : base(messageQueueClient,
                   messagePublisher,
                   handlerProvider,
                   commandQueueName,
                   consumerId,
                   consumerConfig)
        {
            _eventStore = eventStore;
        }

        protected override async Task ConsumeMessage(IMessageContext commandContext)
        {
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                var command = commandContext.Message as ICommand;
                var needReply = !string.IsNullOrEmpty(commandContext.ReplyToEndPoint);
                var sagaInfo = commandContext.SagaInfo;
                if (command == null)
                {
                    return;
                }

                using var scope = ObjectProviderFactory.Instance
                                                       .ObjectProvider
                                                       .CreateScope(builder => builder.RegisterInstance(typeof(IMessageContext), commandContext));
                using (Logger.BeginScope(new {
                    commandContext.Topic,
                    commandContext.MessageId,
                    commandContext.Key
                }))
                {
                    var messageStore = scope.GetService<IMessageStore>();
                    var eventMessageStates = new List<MessageState>();
                    var commandHandledInfo = await messageStore.GetCommandHandledInfoAsync(commandContext.MessageId)
                                                               .ConfigureAwait(false);
                    IMessageContext messageReply = null;
                    if (commandHandledInfo != null)
                    {
                        if (needReply)
                        {
                            messageReply = MessageQueueClient.WrapMessage(commandHandledInfo.Result,
                                                                          commandContext.MessageId,
                                                                          commandContext.ReplyToEndPoint, 
                                                                          producer: Producer,
                                                                          messageId: ObjectId.GenerateNewId()
                                                                                             .ToString());
                            eventMessageStates.Add(new MessageState(messageReply));
                        }
                    }
                    else
                    {
                        var eventBus = scope.GetService<IEventBus>();
                        var messageHandlerType = HandlerProvider.GetHandlerTypes(command.GetType()).FirstOrDefault();
                        Logger.LogInformation("Handle command, commandID:{0}", commandContext.MessageId);

                        if (messageHandlerType == null)
                        {
                            Logger.LogDebug($"command has no handlerTypes, message:{command.ToJson()}");
                            if (needReply)
                            {
                                messageReply = MessageQueueClient.WrapMessage(new NoHandlerExists(),
                                                                              commandContext.MessageId,
                                                                              commandContext.ReplyToEndPoint, producer: Producer);
                                eventMessageStates.Add(new MessageState(messageReply));
                            }
                        }
                        else
                        {
                            try
                            {
                                var messageHandler = scope.GetRequiredService(messageHandlerType.Type);

                                using var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                                                  new TransactionOptions {IsolationLevel = IsolationLevel.ReadCommitted},
                                                                                  TransactionScopeAsyncFlowOption.Enabled);
                                if (messageHandlerType.IsAsync)
                                {
                                    await ((dynamic) messageHandler).Handle((dynamic) command)
                                                                    .ConfigureAwait(false);
                                }
                                else
                                {
                                    var handler = messageHandler;
                                    await Task.Run(() => { ((dynamic) handler).Handle((dynamic) command); }).ConfigureAwait(false);
                                }

                                if (needReply)
                                {
                                    messageReply = MessageQueueClient.WrapMessage(commandContext.Reply,
                                                                                  commandContext.MessageId,
                                                                                  commandContext.ReplyToEndPoint,
                                                                                  producer: Producer);
                                    eventMessageStates.Add(new MessageState(messageReply));
                                }

                                eventBus.GetEvents()
                                        .ForEach(@event =>
                                        {
                                            var topic = @event.GetFormatTopic();
                                            var eventContext = MessageQueueClient.WrapMessage(@event,
                                                                                              commandContext.MessageId,
                                                                                              topic,
                                                                                              @event.Key,
                                                                                              sagaInfo: sagaInfo,
                                                                                              producer: Producer);
                                            eventMessageStates.Add(new MessageState(eventContext));
                                        });

                                eventBus.GetToPublishAnywayMessages()
                                        .ForEach(@event =>
                                        {
                                            var topic = @event.GetFormatTopic();
                                            var eventContext = MessageQueueClient.WrapMessage(@event,
                                                                                              commandContext.MessageId,
                                                                                              topic,
                                                                                              @event.Key,
                                                                                              sagaInfo: sagaInfo,
                                                                                              producer: Producer);
                                            eventMessageStates.Add(new MessageState(eventContext));
                                        });

                                eventMessageStates.AddRange(GetSagaReplyMessageStates(sagaInfo, eventBus));

                                await messageStore.SaveCommandAsync(commandContext, commandContext.Reply,
                                                                    eventMessageStates.Select(s => s.MessageContext).ToArray())
                                                  .ConfigureAwait(false);
                                transactionScope.Complete();
                            }
                            catch (Exception e)
                            {
                                eventMessageStates.Clear();
                                messageStore.Rollback();


                                if (needReply)
                                {
                                    messageReply = MessageQueueClient.WrapMessage(e,
                                                                                  commandContext.MessageId,
                                                                                  commandContext.ReplyToEndPoint,
                                                                                  producer: Producer,
                                                                                  messageId: ObjectId.GenerateNewId().ToString());
                                    eventMessageStates.Add(new MessageState(messageReply));
                                }

                                eventBus.GetToPublishAnywayMessages()
                                        .ForEach(@event =>
                                        {
                                            var topic = @event.GetFormatTopic();
                                            var eventContext = MessageQueueClient.WrapMessage(@event,
                                                                                              commandContext.MessageId,
                                                                                              topic,
                                                                                              @event.Key,
                                                                                              sagaInfo: sagaInfo,
                                                                                              producer: Producer);
                                            eventMessageStates.Add(new MessageState(eventContext));
                                        });
                                if (e is DomainException exception)
                                {
                                    var domainExceptionEvent = exception.DomainExceptionEvent;
                                    if (domainExceptionEvent != null)
                                    {
                                        var topic = domainExceptionEvent.GetFormatTopic();

                                        var exceptionMessage = MessageQueueClient.WrapMessage(domainExceptionEvent,
                                                                                              commandContext.MessageId,
                                                                                              topic,
                                                                                              producer: Producer);
                                        eventMessageStates.Add(new MessageState(exceptionMessage));
                                    }

                                    Logger.LogWarning(e, command.ToJson());
                                }
                                else
                                {
                                    Logger.LogError(e, command.ToJson());
                                    //if we meet with unknown exception, we interrupt saga
                                    if (sagaInfo != null)
                                    {
                                        eventBus.FinishSaga(e);
                                    }
                                }

                                eventMessageStates.AddRange(GetSagaReplyMessageStates(sagaInfo, eventBus));
                                await messageStore.SaveFailedCommandAsync(commandContext, e,
                                                                          eventMessageStates.Select(s => s.MessageContext)
                                                                                            .ToArray())
                                                  .ConfigureAwait(false);
                            }
                        }
                    }

                    if (eventMessageStates.Count > 0)
                    {
                        var sendTask = MessagePublisher.SendAsync(CancellationToken.None,
                                                                  eventMessageStates.ToArray());
                        // we don't need to wait the send task complete here.
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, $"{ConsumerId} CommandProcessor consume command failed");
            }
            finally
            {
                watch.Stop();
                Logger.LogDebug($"{commandContext.ToJson()} consumed cost:{watch.ElapsedMilliseconds}ms");
                InternalConsumer.CommitOffset(commandContext);
            }
        }
    }
}