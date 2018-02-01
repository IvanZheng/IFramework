using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.DependencyInjection;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace IFramework.Command.Impl
{
    public class CommandConsumer : IMessageConsumer
    {
        protected CancellationTokenSource CancellationTokenSource;
        protected string CommandQueueName;
        protected string ConsumerId;
        protected IHandlerProvider HandlerProvider;
        protected ICommitOffsetable InternalConsumer;
        protected ILogger Logger;
        //protected Task _consumeMessageTask;
        protected MessageProcessor MessageProcessor;
        protected IMessagePublisher MessagePublisher;
        protected IMessageQueueClient MessageQueueClient;
        protected ConsumerConfig ConsumerConfig;
        private string _producer;

        public CommandConsumer(IMessageQueueClient messageQueueClient,
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
            CancellationTokenSource = new CancellationTokenSource();
            MessageQueueClient = messageQueueClient;
            MessageProcessor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>(),
                                                     ConsumerConfig.MailboxProcessBatchCount);
            Logger = IoCFactory.Resolve<ILoggerFactory>().Create(GetType().Name);
        }

        public string Producer => _producer ?? (_producer = $"{CommandQueueName}.{ConsumerId}");

        public void Start()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(CommandQueueName))
                {
                    InternalConsumer = MessageQueueClient.StartQueueClient(CommandQueueName, ConsumerId,
                                                                             OnMessageReceived,
                                                                             ConsumerConfig);
                }
                MessageProcessor.Start();
            }
            catch (Exception e)
            {
                Logger?.Error(e.GetBaseException().Message, e);
            }
        }

        public void Stop()
        {
            InternalConsumer.Stop();
            MessageProcessor.Stop();
        }

        public string GetStatus()
        {
            return ToString();
        }

        public decimal MessageCount { get; set; }

        protected void OnMessageReceived(params IMessageContext[] messageContexts)
        {
            messageContexts.ForEach(messageContext =>
            {
                MessageProcessor.Process(messageContext, ConsumeMessage);
                MessageCount++;
            });
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
                                                                                sagaInfo: sagaInfo, producer: Producer);
                                eventMessageStates.Add(new MessageState(sagaReply));
                            }
                        });
            }
            return eventMessageStates;
        }

        protected virtual async Task ConsumeMessage(IMessageContext commandContext)
        {
            try
            {
                var command = commandContext.Message as ICommand;
                var needReply = !string.IsNullOrEmpty(commandContext.ReplyToEndPoint);
                var sagaInfo = commandContext.SagaInfo;
                if (command == null)
                {
                    InternalConsumer.CommitOffset(commandContext);
                    return;
                }
                var needRetry = command.NeedRetry;

                using (var scope = IoCFactory.Instance
                                             .ObjectProvider
                                             .CreateScope(builder => builder.RegisterInstance(typeof(IMessageContext), commandContext)))
                {
                    var messageStore = scope.GetService<IMessageStore>();
                    var eventMessageStates = new List<MessageState>();
                    var commandHandledInfo = messageStore.GetCommandHandledInfo(commandContext.MessageID);
                    IMessageContext messageReply = null;
                    if (commandHandledInfo != null)
                    {
                        if (needReply)
                        {
                            messageReply = MessageQueueClient.WrapMessage(commandHandledInfo.Result,
                                                                           commandContext.MessageID,
                                                                           commandContext.ReplyToEndPoint, producer: Producer,
                                                                           messageId: ObjectId.GenerateNewId().ToString());
                            eventMessageStates.Add(new MessageState(messageReply));
                        }
                    }
                    else
                    {
                        var eventBus = scope.GetService<IEventBus>();
                        var messageHandlerType = HandlerProvider.GetHandlerTypes(command.GetType()).FirstOrDefault();
                        Logger?.InfoFormat("Handle command, commandID:{0}", commandContext.MessageID);

                        if (messageHandlerType == null)
                        {
                            Logger?.Debug($"command has no handlerTypes, message:{command.ToJson()}");
                            if (needReply)
                            {
                                messageReply = MessageQueueClient.WrapMessage(new NoHandlerExists(),
                                                                               commandContext.MessageID,
                                                                               commandContext.ReplyToEndPoint, producer: Producer);
                                eventMessageStates.Add(new MessageState(messageReply));
                            }
                        }
                        else
                        {
                            object messageHandler = null;
                            do
                            {
                                try
                                {
                                    if (messageHandler == null)
                                    {
                                        messageHandler = scope.GetService(messageHandlerType.Type);
                                    }

                                    using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                                                       new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                                                                                       TransactionScopeAsyncFlowOption.Enabled))
                                    {
                                        if (messageHandlerType.IsAsync)
                                        {
                                            await ((dynamic)messageHandler).Handle((dynamic)command)
                                                                           .ConfigureAwait(false);
                                        }
                                        else
                                        {
                                            var handler = messageHandler;
                                            await Task.Run(() => { ((dynamic)handler).Handle((dynamic)command); }).ConfigureAwait(false);
                                        }
                                        if (needReply)
                                        {
                                            messageReply = MessageQueueClient.WrapMessage(commandContext.Reply,
                                                                                           commandContext.MessageID, commandContext.ReplyToEndPoint,
                                                                                           producer: Producer);
                                            eventMessageStates.Add(new MessageState(messageReply));
                                        }

                                        eventBus.GetEvents()
                                                .ForEach(@event =>
                                                {
                                                    var topic = @event.GetFormatTopic();
                                                    var eventContext = MessageQueueClient.WrapMessage(@event,
                                                                                                       commandContext.MessageID, topic, @event.Key,
                                                                                                       sagaInfo: sagaInfo, producer: Producer);
                                                    eventMessageStates.Add(new MessageState(eventContext));
                                                });

                                        eventBus.GetToPublishAnywayMessages()
                                                .ForEach(@event =>
                                                {
                                                    var topic = @event.GetFormatTopic();
                                                    var eventContext = MessageQueueClient.WrapMessage(@event,
                                                                                                       commandContext.MessageID, topic,
                                                                                                       @event.Key, sagaInfo: sagaInfo, producer: Producer);
                                                    eventMessageStates.Add(new MessageState(eventContext));
                                                });

                                        eventMessageStates.AddRange(GetSagaReplyMessageStates(sagaInfo, eventBus));

                                        messageStore.SaveCommand(commandContext, commandContext.Reply,
                                                                 eventMessageStates.Select(s => s.MessageContext).ToArray());
                                        transactionScope.Complete();
                                    }
                                    needRetry = false;
                                }
                                catch (Exception e)
                                {
                                    eventMessageStates.Clear();
                                    messageStore.Rollback();

                                    if (e is DBConcurrencyException && needRetry)
                                    {
                                        eventBus.ClearMessages();
                                    }
                                    else
                                    {
                                        if (needReply)
                                        {
                                            messageReply = MessageQueueClient.WrapMessage(e.GetBaseException(),
                                                                                           commandContext.MessageID,
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
                                                                                                       commandContext.MessageID,
                                                                                                       topic, @event.Key, sagaInfo: sagaInfo, producer: Producer);
                                                    eventMessageStates.Add(new MessageState(eventContext));
                                                });
                                        if (e is DomainException)
                                        {
                                            var domainExceptionEvent = ((DomainException)e).DomainExceptionEvent;
                                            if (domainExceptionEvent != null)
                                            {
                                                var topic = domainExceptionEvent.GetFormatTopic();

                                                var exceptionMessage = MessageQueueClient.WrapMessage(domainExceptionEvent,
                                                                                                       commandContext.MessageID,
                                                                                                       topic,
                                                                                                       producer: Producer);
                                                eventMessageStates.Add(new MessageState(exceptionMessage));
                                            }
                                            Logger?.Warn(command.ToJson(), e);
                                        }
                                        else
                                        {
                                            Logger?.Error(command.ToJson(), e);
                                            //if we meet with unknown exception, we interrupt saga
                                            if (sagaInfo != null)
                                            {
                                                eventBus.FinishSaga(e);
                                            }
                                        }
                                        eventMessageStates.AddRange(GetSagaReplyMessageStates(sagaInfo, eventBus));
                                        messageStore.SaveFailedCommand(commandContext, e,
                                                                       eventMessageStates.Select(s => s.MessageContext).ToArray());
                                        needRetry = false;
                                    }
                                }
                            } while (needRetry);
                        }
                    }
                    if (eventMessageStates.Count > 0)
                    {
                        var sendTask = MessagePublisher.SendAsync(eventMessageStates.ToArray());
                        // we don't need to wait the send task complete here.
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.Fatal($"consume command failed", ex);
            }
            InternalConsumer.CommitOffset(commandContext);
        }
    }
}