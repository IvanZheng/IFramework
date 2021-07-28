using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace IFramework.Command.Impl
{
    public class CommandProcessor : IMessageProcessor
    {
        private string _producer;
        protected string CommandQueueName;
        protected ConsumerConfig ConsumerConfig;
        protected string ConsumerId;
        protected IHandlerProvider HandlerProvider;
        protected IMessageConsumer InternalConsumer;
        protected ILogger Logger;
        protected MailboxProcessor MessageProcessor;
        protected IMessagePublisher MessagePublisher;
        protected IMessageQueueClient MessageQueueClient;

        public CommandProcessor(IMessageQueueClient messageQueueClient,
                                IMessagePublisher messagePublisher,
                                IHandlerProvider handlerProvider,
                                string commandQueueName,
                                string consumerId,
                                ConsumerConfig consumerConfig = null)
        {
            ConsumerConfig = consumerConfig ?? ConsumerConfig.DefaultConfig;
            CommandQueueName = commandQueueName;
            HandlerProvider = handlerProvider;
            MessagePublisher = messagePublisher;
            ConsumerId = consumerId;
            MessageQueueClient = messageQueueClient;
            var loggerFactory = ObjectProviderFactory.GetService<ILoggerFactory>();
            MessageProcessor = new MailboxProcessor(new DefaultProcessingMessageScheduler(),
                                                    new OptionsWrapper<MailboxOption>(new MailboxOption
                                                    {
                                                        BatchCount = ConsumerConfig.MailboxProcessBatchCount
                                                    }),
                                                    loggerFactory.CreateLogger<MailboxProcessor>());
            Logger = loggerFactory.CreateLogger(GetType());
        }

        public string Producer => _producer ?? (_producer = $"{CommandQueueName}{FrameworkConfigurationExtension.QueueNameSplit}{ConsumerId}");

        public void Start()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(CommandQueueName))
                {
                    InternalConsumer = MessageQueueClient.StartQueueClient(CommandQueueName,
                                                                           ConsumerId,
                                                                           OnMessagesReceived,
                                                                           ConsumerConfig);
                }

                MessageProcessor.Start();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Command Consumer {ConsumerId} Start Failed");
            }
        }

        public void Stop()
        {
            InternalConsumer.Stop();
            MessageProcessor.Stop();
        }

        public string GetStatus()
        {
            return $"{Producer}: {InternalConsumer?.Status}";
        }

        public decimal MessageCount { get; set; }

        protected void OnMessagesReceived(CancellationToken cancellationToken, params IMessageContext[] messageContexts)
        {
            messageContexts.ForEach(messageContext =>
            {
                try
                {
                    MessageProcessor.Process(messageContext.Key, () => ConsumeMessage(messageContext, cancellationToken));
                    MessageCount++;
                }
                catch (Exception e)
                {
                    InternalConsumer.CommitOffset(messageContext);
                    Logger.LogError(e, $"failed to process command: {messageContext.MessageOffset.ToJson()}");
                }
            });
        }

        protected void GetSagaReplyMessageState(List<MessageState> messageStates, SagaInfo sagaInfo, IEventBus eventBus)
        {
            var sagaResult = eventBus.GetSagaResult();
            if (sagaInfo != null && !string.IsNullOrWhiteSpace(sagaInfo.SagaId) && sagaResult != null)
            {
                
                            var topic = sagaInfo.ReplyEndPoint;
                            if (!string.IsNullOrEmpty(topic))
                            {
                                var sagaReply = MessageQueueClient.WrapMessage(sagaResult,
                                                                               topic: topic,
                                                                               messageId: ObjectId.GenerateNewId().ToString(),
                                                                               sagaInfo: sagaInfo, 
                                                                               producer: Producer);
                                messageStates.Add(new MessageState(sagaReply));
                            }
            }
        }

        protected virtual async Task ConsumeMessage(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            await Task.Yield();
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
                using (var scope = ObjectProviderFactory.Instance
                                                        .ObjectProvider
                                                        .CreateScope(builder => builder.RegisterInstance(typeof(IMessageContext), commandContext)))
                {
                    using (Logger.BeginScope(new
                    {
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
                                                                              commandContext.ReplyToEndPoint, producer: Producer,
                                                                              messageId: ObjectId.GenerateNewId().ToString());
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

                                    await messageStore.ExecuteInTransactionAsync(async () =>
                                    {
                                         //using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                         //                                              new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                                         //                                              TransactionScopeAsyncFlowOption.Enabled))
                                         {
                                             if (messageHandlerType.IsAsync)
                                             {
                                                 await ((dynamic)messageHandler).Handle((dynamic)command, cancellationToken)
                                                                                 .ConfigureAwait(false);
                                             }
                                             else
                                             {
                                                 var handler = messageHandler;
                                                 ((dynamic)handler).Handle((dynamic)command);
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
                                         
                                             GetSagaReplyMessageState(eventMessageStates, sagaInfo, eventBus);
                                         
                                             await messageStore.SaveCommandAsync(commandContext, commandContext.Reply,
                                                                      eventMessageStates.Select(s => s.MessageContext).ToArray())
                                                               .ConfigureAwait(false);
                                             //transactionScope.Complete();
                                         }
                                    }, cancellationToken);

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

                                    GetSagaReplyMessageState(eventMessageStates, sagaInfo, eventBus);
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