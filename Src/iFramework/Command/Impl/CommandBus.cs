using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.IoC;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;

namespace IFramework.Command.Impl
{
    public class CommandBus: MessageSender, ICommandBus
    {
        /// <summary>
        ///     cache command states for command reply. When reply comes, make replyTaskCompletionSouce completed
        /// </summary>
        private readonly ConcurrentDictionary<string, MessageState> _commandStateQueues;

        private readonly string _consumerId;

        //protected string[] _commandQueueNames;
        private readonly ILinearCommandManager _linearCommandManager;

        private readonly MessageProcessor _messageProcessor;
        private readonly string _replySubscriptionName;
        private readonly string _replyTopicName;
        private ICommitOffsetable _internalConsumer;
        private readonly ConsumerConfig _consumerConfig;

        public CommandBus(IMessageQueueClient messageQueueClient,
                          ILinearCommandManager linearCommandManager,
                          string consumerId,
                          string replyTopicName,
                          string replySubscriptionName,
                          ConsumerConfig consumerConfig = null)
            : base(messageQueueClient)
        {
            _consumerConfig = consumerConfig ?? ConsumerConfig.DefaultConfig;
            _consumerId = consumerId;
            _commandStateQueues = new ConcurrentDictionary<string, MessageState>();
            _linearCommandManager = linearCommandManager;
            _replyTopicName = Configuration.Instance.FormatAppName(replyTopicName);
            _replySubscriptionName = Configuration.Instance.FormatAppName(replySubscriptionName);
            // _commandQueueNames = commandQueueNames;
            _messageProcessor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>(),
                                                     _consumerConfig.MailboxProcessBatchCount);
        }

        public override void Start()
        {
            base.Start();

            #region init process command reply worker

            try
            {
                if (!string.IsNullOrWhiteSpace(_replyTopicName))
                {
                    _internalConsumer = _messageQueueClient.StartSubscriptionClient(_replyTopicName,
                                                                                    _replySubscriptionName,
                                                                                    _consumerId,
                                                                                    OnMessagesReceived,
                                                                                    _consumerConfig);
                }
            }
            catch (Exception e)
            {
                _logger?.Error(e.GetBaseException().Message, e);
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
            SagaInfo sagaInfo = null;
            if (!string.IsNullOrEmpty(sagaId))
            {
                sagaInfo = new SagaInfo { SagaId = sagaId, ReplyEndPoint = _replyTopicName };
            }
            var commandContext = WrapCommand(command, false, sagaInfo);
            var commandState = BuildCommandState(commandContext, sendCancellationToken, sendTimeout,
                                                 replyCancellationToken, true);
            _commandStateQueues.GetOrAdd(sagaId, commandState);
            return (await SendAsync(commandState).ConfigureAwait(false))
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

            _logger.Debug($"enter in command bus send {commandState.MessageID} topic:{commandState.MessageContext.Topic}");
            if (needReply)
            {
                _commandStateQueues.GetOrAdd(commandState.MessageID, commandState);
            }
            return (await SendAsync(commandState).ConfigureAwait(false))
                .FirstOrDefault();
        }

        public IMessageContext WrapCommand(ICommand command,
                                           bool needReply,
                                           SagaInfo sagaInfo = null,
                                           string producer = null)
        {
            if (string.IsNullOrEmpty(command.ID))
            {
                _logger?.Error(new NoMessageId());
                throw new NoMessageId();
            }
            string commandKey = null;
            if (command is ILinearCommand)
            {
                var linearKey = _linearCommandManager.GetLinearKey(command as ILinearCommand);
                if (linearKey != null)
                {
                    commandKey = linearKey.ToString();
                }
            }
            IMessageContext commandContext = null;

            #region pickup a queue to send command

            // move this logic into  concrete messagequeueClient. kafka sdk already implement it. 
            // service bus client still need it.
            //int keyUniqueCode = !string.IsNullOrWhiteSpace(commandKey) ?
            //    commandKey.GetUniqueCode() : command.ID.GetUniqueCode();
            //var queue = _commandQueueNames[Math.Abs(keyUniqueCode % _commandQueueNames.Length)];

            #endregion

            commandContext = _messageQueueClient.WrapMessage(command,
                                                             key: commandKey,
                                                             replyEndPoint: needReply ? _replyTopicName : null,
                                                             sagaInfo: sagaInfo, producer: producer ?? _consumerId);
            if (string.IsNullOrEmpty(commandContext.Topic))
            {
                throw new NoCommandTopic();
            }
            return commandContext;
        }

        public void SendMessageStates(IEnumerable<MessageState> commandStates)
        {
            SendAsync(commandStates.ToArray());
        }

        protected override IEnumerable<IMessageContext> GetAllUnSentMessages()
        {
            using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
            using (var messageStore = scope.Resolve<IMessageStore>())
            {
                return messageStore.GetAllUnSentCommands((messageId,
                                                          message,
                                                          topic,
                                                          correlationId,
                                                          replyEndPoint,
                                                          sagaInfo,
                                                          producer) =>
                                                             _messageQueueClient.WrapMessage(message, key: message.Key, topic: topic,
                                                                                             messageId: messageId, correlationId: correlationId,
                                                                                             replyEndPoint: replyEndPoint,
                                                                                             sagaInfo: sagaInfo, producer: producer));
            }
        }

        protected override async Task SendMessageStateAsync(MessageState messageState, CancellationToken cancellationToken)
        {
            _logger.Debug($"send message start msgId: {messageState.MessageID} topic:{messageState.MessageContext.Topic}");
            var messageContext = messageState.MessageContext;
            await _messageQueueClient.SendAsync(messageContext, messageContext.Topic ?? _defaultTopic, cancellationToken);
            _logger.Debug($"send message complete msgId: {messageState.MessageID} topic:{messageState.MessageContext.Topic}");
            CompleteSendingMessage(messageState);
        }

        protected override void CompleteSendingMessage(MessageState messageState)
        {
            messageState?.SendTaskCompletionSource?
                .TrySetResult(new MessageResponse(messageState.MessageContext,
                                                  messageState.ReplyTaskCompletionSource?.Task,
                                                  messageState.NeedReply));

            if (_needMessageStore && messageState != null)
            {
                Task.Run(() =>
                {
                    using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
                    using (var messageStore = scope.Resolve<IMessageStore>())
                    {
                        messageStore.RemoveSentCommand(messageState.MessageID);
                    }
                });
            }
        }

        protected void OnMessagesReceived(params IMessageContext[] replies)
        {
            replies.ForEach(reply => { _messageProcessor.Process(reply, ConsumeReply); });
        }

        protected async Task ConsumeReply(IMessageContext reply)
        {
            await Task.Run(() =>
                      {
                          _logger?.InfoFormat("Handle reply:{0} content:{1}", reply.MessageID, reply.ToJson());
                          var sagaInfo = reply.SagaInfo;
                          var correlationID = sagaInfo?.SagaId ?? reply.CorrelationID;
                          var messageState = _commandStateQueues.TryGetValue(correlationID);
                          if (messageState != null)
                          {
                              _commandStateQueues.TryRemove(correlationID);
                              if (reply.Message is Exception)
                              {
                                  messageState.ReplyTaskCompletionSource.TrySetException(reply.Message as Exception);
                              }
                              else
                              {
                                  messageState.ReplyTaskCompletionSource.TrySetResult(reply.Message);
                              }
                          }
                          _internalConsumer.CommitOffset(reply);
                      })
                      .ConfigureAwait(false);
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

            TaskCompletionSource<object> replyTaskCompletionSource = null;
            MessageState commandState = null;
            if (needReply)
            {
                replyTaskCompletionSource = new TaskCompletionSource<object>();
                commandState = new MessageState(commandContext, sendTaskCompletionSource, replyTaskCompletionSource,
                                                needReply);
                if (replyCancellationToken != CancellationToken.None)
                {
                    replyCancellationToken.Register(OnReplyCancel, commandState);
                }
            }
            else
            {
                commandState = new MessageState(commandContext, sendTaskCompletionSource, needReply);
            }
            return commandState;
        }

        protected void OnSendTimeout(object state)
        {
            var sendTaskCompletionSource = state as TaskCompletionSource<MessageResponse>;
            if (sendTaskCompletionSource != null)
            {
                sendTaskCompletionSource.TrySetException(new TimeoutException());
            }
        }


        protected void OnReplyCancel(object state)
        {
            var messageState = state as MessageState;
            if (messageState != null)
            {
                messageState.ReplyTaskCompletionSource.TrySetCanceled();
                _commandStateQueues.TryRemove(messageState.MessageID);
            }
        }
    }
}