using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFramework.Command.Impl
{
    public class CommandBus : MessageSender, ICommandBus
    {
        /// <summary>
        ///     cache command states for command reply. When reply comes, make replyTaskCompletionSouce completed
        /// </summary>
        private readonly ConcurrentDictionary<string, MessageState> _commandStateQueues;

        private readonly ConsumerConfig _consumerConfig;

        private readonly string _consumerId;

        //protected string[] _commandQueueNames;
        private readonly ISerialCommandManager _serialCommandManager;

        private readonly MailboxProcessor _messageProcessor;
        private readonly string _replySubscriptionName;
        private readonly string _replyTopicName;
        private IMessageConsumer _internalConsumer;

        public CommandBus(IMessageQueueClient messageQueueClient,
                          ISerialCommandManager serialCommandManager,
                          string consumerId,
                          string replyTopicName,
                          string replySubscriptionName,
                          ConsumerConfig consumerConfig = null)
            : base(messageQueueClient)
        {
            _consumerConfig = consumerConfig ?? ConsumerConfig.DefaultConfig;
            _consumerId = consumerId;
            _commandStateQueues = new ConcurrentDictionary<string, MessageState>();
            _serialCommandManager = serialCommandManager;
            _replyTopicName = Configuration.Instance.FormatAppName(replyTopicName);
            _replySubscriptionName = Configuration.Instance.FormatAppName(replySubscriptionName);
            // _commandQueueNames = commandQueueNames;
            _messageProcessor = new MailboxProcessor(new DefaultProcessingMessageScheduler(),
                                                     new OptionsWrapper<MailboxOption>(new MailboxOption
                                                     {
                                                         BatchCount = _consumerConfig.MailboxProcessBatchCount
                                                     }),
                                                     ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger<MailboxProcessor>());
        }

        public override void Start()
        {
            base.Start();

            #region init process command reply worker

            try
            {
                if (!string.IsNullOrWhiteSpace(_replyTopicName))
                {
                    _internalConsumer = MessageQueueClient.StartSubscriptionClient(_replyTopicName,
                                                                                   _replySubscriptionName,
                                                                                   _consumerId,
                                                                                   OnMessagesReceived,
                                                                                   _consumerConfig);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"command bus started faield");
            }

            #endregion

            _messageProcessor.Start();
        }

        public override void Stop()
        {
            base.Stop();
            _internalConsumer.Stop();
            _messageProcessor.Stop();
        }

        public Task<MessageResponse> SendAsync(ICommand command, bool needReply = false)
        {
            return SendAsync(command, CancellationToken.None, TimerTaskFactory.Infinite,
                             CancellationToken.None,
                             needReply);
        }

        public Task<MessageResponse> SendAsync(ICommand command, TimeSpan timeout, bool needReply = false)
        {
            return SendAsync(command, CancellationToken.None, timeout, CancellationToken.None, needReply);
        }

        public Task<MessageResponse> StartSaga(ICommand command, string sageId = null)
        {
            return StartSaga(command, CancellationToken.None, TimerTaskFactory.Infinite,
                             CancellationToken.None,
                             sageId);
        }

        public Task<MessageResponse> StartSaga(ICommand command, TimeSpan timeout, string sageId = null)
        {
            return StartSaga(command, CancellationToken.None, timeout, CancellationToken.None, sageId);
        }

        public async Task<MessageResponse> StartSaga(ICommand command,
                                                     CancellationToken sendCancellationToken,
                                                     TimeSpan sendTimeout,
                                                     CancellationToken replyCancellationToken,
                                                     string sagaId = null)
        {
            sagaId = sagaId ?? ObjectId.GenerateNewId().ToString();
            var sagaInfo = new SagaInfo { SagaId = sagaId, ReplyEndPoint = _replyTopicName };

            var commandContext = WrapCommand(command, false, sagaInfo);
            var commandState = BuildCommandState(commandContext,
                                                 sendCancellationToken,
                                                 sendTimeout,
                                                 replyCancellationToken, true);
            _commandStateQueues.GetOrAdd(sagaId, commandState);
            return (await SendAsync(sendCancellationToken, commandState).ConfigureAwait(false))
                .FirstOrDefault();
        }

        public async Task<MessageResponse> SendAsync(ICommand command,
                                                     CancellationToken sendCancellationToken,
                                                     TimeSpan timeout,
                                                     CancellationToken replyCancellationToken,
                                                     bool needReply = false)
        {
            var commandContext = WrapCommand(command, needReply);
            var commandState = BuildCommandState(commandContext, sendCancellationToken, timeout, replyCancellationToken,
                                                 needReply);

            Logger.LogDebug($"enter in command bus send {commandState.MessageID} topic:{commandState.MessageContext.Topic}");
            if (needReply)
            {
                _commandStateQueues.GetOrAdd(commandState.MessageID, commandState);
            }
            return (await SendAsync(sendCancellationToken, commandState).ConfigureAwait(false))
                .FirstOrDefault();
        }

        public IMessageContext WrapCommand(ICommand command,
                                           bool needReply,
                                           SagaInfo sagaInfo = null,
                                           string producer = null)
        {
            if (string.IsNullOrEmpty(command.Id))
            {
                var noMessageIdException = new NoMessageId();
                Logger.LogError(noMessageIdException, $"{command.ToJson()}");
                throw noMessageIdException;
            }
            string commandKey = command.Key;
            if (command is ILinearCommand linearCommand)
            {
                var linearKey = _serialCommandManager.GetLinearKey(linearCommand);
                if (linearKey != null)
                {
                    commandKey = linearKey.ToString();
                    command.Key = commandKey;
                }
            }
            IMessageContext commandContext = null;

            #region pickup a queue to send command

            // move this logic into  concrete messagequeueClient. kafka sdk already implement it. 
            // service bus client still need it.
            //int keyUniqueCode = !string.IsNullOrWhiteSpace(commandKey) ?
            //    commandKey.GetUniqueCode() : command.Id.GetUniqueCode();
            //var queue = _commandQueueNames[Math.Abs(keyUniqueCode % _commandQueueNames.Length)];

            #endregion

            var topic = command.GetFormatTopic();
            commandContext = MessageQueueClient.WrapMessage(command,
                                                            topic:topic,
                                                            key: commandKey,
                                                            replyEndPoint: needReply ? _replyTopicName : null,
                                                            sagaInfo: sagaInfo,
                                                            producer: producer ?? _consumerId);
            if (string.IsNullOrEmpty(commandContext.Topic))
            {
                throw new NoCommandTopic();
            }
            return commandContext;
        }

        public void SendMessageStates(IEnumerable<MessageState> commandStates)
        {
            SendAsync(CancellationToken.None, commandStates.ToArray());
        }

        protected override IEnumerable<IMessageContext> GetAllUnSentMessages()
        {
            using (var scope = ObjectProviderFactory.Instance.ObjectProvider.CreateScope())
            using (var messageStore = scope.GetService<IMessageStore>())
            {
                return messageStore.GetAllUnSentCommands((messageId,
                                                          message,
                                                          topic,
                                                          correlationId,
                                                          replyEndPoint,
                                                          sagaInfo,
                                                          producer) =>
                                                             MessageQueueClient.WrapMessage(message, key: message.Key, topic: topic,
                                                                                            messageId: messageId, correlationId: correlationId,
                                                                                            replyEndPoint: replyEndPoint,
                                                                                            sagaInfo: sagaInfo, producer: producer));
            }
        }

        protected override async Task SendMessageStateAsync(MessageState messageState, CancellationToken cancellationToken)
        {
            Logger.LogDebug($"send message start msgId: {messageState.MessageID} topic:{messageState.MessageContext.Topic}");
            var messageContext = messageState.MessageContext;
            await MessageQueueClient.SendAsync(messageContext, messageContext.Topic ?? DefaultTopic, cancellationToken);
            Logger.LogDebug($"send message complete msgId: {messageState.MessageID} topic:{messageState.MessageContext.Topic}");
            CompleteSendingMessage(messageState);
        }

        protected override void CompleteSendingMessage(MessageState messageState)
        {
            messageState?.SendTaskCompletionSource?
                .TrySetResult(new MessageResponse(messageState.MessageContext,
                                                  messageState.ReplyTaskCompletionSource?.Task,
                                                  messageState.NeedReply));

            if (NeedMessageStore && messageState != null)
            {
                ObjectProviderFactory.Instance
                                     .ObjectProvider
                                     .GetService<IMessageStoreDaemon>()
                                     .RemoveSentCommand(messageState.MessageID);
            }
        }

        protected void OnMessagesReceived(CancellationToken cancellationToken, params IMessageContext[] replies)
        {
            replies.ForEach(reply =>
            {
                try
                {
                    _messageProcessor.Process(reply.Key, () => ConsumeReply(reply));
                }
                catch (Exception e)
                {
                    _internalConsumer.CommitOffset(reply);
                    Logger.LogError(e, $"failed to process command: {reply.MessageOffset.ToJson()}");
                }
            });
        }

        protected Task ConsumeReply(IMessageContext reply)
        {
            try
            {
                Logger.LogInformation("Handle reply:{0} content:{1}", reply.MessageId, reply.ToJson());
                var sagaInfo = reply.SagaInfo;
                var correlationId = sagaInfo?.SagaId ?? reply.CorrelationId;
                var messageState = _commandStateQueues.TryGetValue(correlationId);
                if (messageState != null)
                {
                    _commandStateQueues.TryRemove(correlationId);
                    if (reply.Message is Exception exception)
                    {
                        messageState.ReplyTaskCompletionSource.TrySetException(exception);
                    }
                    else
                    {
                        messageState.ReplyTaskCompletionSource.TrySetResult(reply.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            finally
            {
                _internalConsumer.CommitOffset(reply);
            }
            return Task.CompletedTask;
        }

        protected MessageState BuildCommandState(IMessageContext commandContext,
                                                 CancellationToken sendCancellationToken,
                                                 TimeSpan timeout,
                                                 CancellationToken replyCancellationToken,
                                                 bool needReply)
        {
            var sendTaskCompletionSource = new TaskCompletionSource<MessageResponse>();
            if (timeout != TimerTaskFactory.Infinite)
            {
                var timeoutCancellationTokenSource = new CancellationTokenSource(timeout);
                timeoutCancellationTokenSource.Token.Register(OnSendTimeout, sendTaskCompletionSource);
            }

            if (sendCancellationToken != CancellationToken.None)
            {
                sendCancellationToken.Register(OnSendCancel, sendTaskCompletionSource);
            }

            MessageState commandState = null;
            if (needReply)
            {
                var replyTaskCompletionSource = new TaskCompletionSource<object>();
                commandState = new MessageState(commandContext,
                                                sendTaskCompletionSource,
                                                replyTaskCompletionSource,
                                                true);
                if (replyCancellationToken != CancellationToken.None)
                {
                    replyCancellationToken.Register(OnReplyCancel, commandState);
                }
            }
            else
            {
                commandState = new MessageState(commandContext, sendTaskCompletionSource, false);
            }
            return commandState;
        }

        protected void OnSendTimeout(object state)
        {
            if (state is TaskCompletionSource<MessageResponse> sendTaskCompletionSource)
            {
                sendTaskCompletionSource.TrySetException(new TimeoutException());
            }
        }


        protected void OnReplyCancel(object state)
        {
            if (state is MessageState messageState)
            {
                messageState.ReplyTaskCompletionSource.TrySetCanceled();
                _commandStateQueues.TryRemove(messageState.MessageID);
            }
        }
    }
}