using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.UnitOfWork;
using IFramework.Message.Impl;
using System.Collections;
using Microsoft.Practices.Unity;
using System.Threading;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Config;
using IFramework.Infrastructure.Logging;
using IFramework.MessageQueue;
using System.Collections.Concurrent;
using IFramework.SysExceptions;

namespace IFramework.Command.Impl
{
    public class CommandBus : MessageSender, ICommandBus
    {
        protected string _replyTopicName;
        protected string _replySubscriptionName;
        protected string[] _commandQueueNames;
        protected ILinearCommandManager _linearCommandManager;
        protected ConcurrentDictionary<string, MessageState> _commandStateQueues;
        protected bool _needMessageStore;
        public CommandBus(IMessageQueueClient messageQueueClient,
                          ILinearCommandManager linearCommandManager,
                          string[] commandQueueNames,
                          string replyTopicName,
                          string replySubscriptionName,
                          bool needMessageStore = true)
            : base(messageQueueClient)
        {
            _commandStateQueues = new ConcurrentDictionary<string, MessageState>();
            _linearCommandManager = linearCommandManager;
            _replyTopicName = replyTopicName;
            _replySubscriptionName = replySubscriptionName;
            _commandQueueNames = commandQueueNames;
            _needMessageStore = needMessageStore;
        }
        protected override IEnumerable<IMessageContext> GetAllUnSentMessages()
        {
            if (_needMessageStore)
            {
                using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
                using (var messageStore = scope.Resolve<IMessageStore>())
                {
                    return messageStore.GetAllUnSentCommands((messageId, message, topic, correlationId) =>
                                                              _messageQueueClient.WrapMessage(message, topic: topic, messageId: messageId, correlationId: correlationId));
                }
            }
            return null;
        }

        protected override void Send(IMessageContext messageContext, string queue)
        {
            _messageQueueClient.Send(messageContext, queue);
        }

        protected override void CompleteSendingMessage(string messageId)
        {
            if (_needMessageStore)
            {
                Task.Factory.StartNew(() =>
                {
                    using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
                    using (var messageStore = scope.Resolve<IMessageStore>())
                    {
                        messageStore.RemoveSentCommand(messageId);
                    }
                }, 
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default);
            }
        }

        public override void Start()
        {
            base.Start();
            #region init process command reply worker
            try
            {
                if (!string.IsNullOrWhiteSpace(_replyTopicName))
                {
                    _messageQueueClient.StartSubscriptionClient(_replyTopicName, _replySubscriptionName, OnMessageReceived);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.GetBaseException().Message, e);
            }
            #endregion
        }

        public override void Stop()
        {
            base.Stop();
            _messageQueueClient.StopSubscriptionClients();
        }

        protected void OnMessageReceived(IMessageContext reply)
        {
            _logger.InfoFormat("Handle reply:{0} content:{1}", reply.MessageID, reply.ToJson());
            var messageState = _commandStateQueues.TryGetValue(reply.CorrelationID);
            if (messageState != null)
            {
                _commandStateQueues.TryRemove(reply.MessageID);
                if (reply.Message is Exception)
                {
                    messageState.TaskCompletionSource.TrySetException(reply.Message as Exception);
                }
                else
                {
                    messageState.TaskCompletionSource.TrySetResult(reply.Message);
                }
            }
        }

        protected IFramework.Message.MessageState BuildMessageState(IMessageContext messageContext, CancellationToken cancellationToken)
        {
            var pendingRequestsCts = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource
                   .CreateLinkedTokenSource(cancellationToken, pendingRequestsCts.Token);
            cancellationToken = linkedCts.Token;
            var source = new TaskCompletionSource<object>();
            var state = new IFramework.Message.MessageState
            {
                MessageID = messageContext.MessageID,
                TaskCompletionSource = source,
                CancellationToken = cancellationToken,
                MessageContext = messageContext
            };
            return state;
        }

        public Task Send(ICommand command)
        {
            return Send(command, CancellationToken.None);
        }

        public IMessageContext WrapCommand(ICommand command, bool needReply = true)
        {
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
            int keyUniqueCode = !string.IsNullOrWhiteSpace(commandKey) ?
                commandKey.GetUniqueCode() : command.ID.GetUniqueCode();
            var queue = _commandQueueNames[Math.Abs(keyUniqueCode % _commandQueueNames.Length)];
            #endregion
            commandContext = _messageQueueClient.WrapMessage(command, topic: queue,
                                                             key: commandKey,
                                                             replyEndPoint: needReply?  _replyTopicName : null);
            return commandContext;
        }

        public Task Send(ICommand command, CancellationToken cancellationToken)
        {
            var commandContext = WrapCommand(command);
            return SendAsync(commandContext, cancellationToken);
        }

        //public void Add(ICommand command)
        //{
        //    var currentMessageContext = PerMessageContextLifetimeManager.CurrentMessageContext;
        //    if (currentMessageContext == null)
        //    {
        //        throw new CurrentMessageContextIsNull();
        //    }
        //    string commandKey = null;
        //    if (command is ILinearCommand)
        //    {
        //        var linearKey = _linearCommandManager.GetLinearKey(command as ILinearCommand);
        //        if (linearKey != null)
        //        {
        //            commandKey = linearKey.ToString();
        //        }
        //    }
        //    #region pickup a queue to send command
        //    int keyUniqueCode = !string.IsNullOrWhiteSpace(commandKey) ?
        //        commandKey.GetUniqueCode() : command.ID.GetUniqueCode();
        //    var queue = _commandQueueNames[Math.Abs(keyUniqueCode % _commandQueueNames.Length)];
        //    #endregion
        //    var commandContext = _messageQueueClient.WrapMessage(command, topic: queue,
        //                                                     key: commandKey,
        //                                                     replyEndPoint: _replyTopicName);
        //    currentMessageContext.ToBeSentMessageContexts.Add(commandContext);
        //}

        public Task<TResult> Send<TResult>(ICommand command)
        {
            return Send<TResult>(command, CancellationToken.None);
        }

        public Task<TResult> Send<TResult>(ICommand command, CancellationToken cancellationToken)
        {
            return Send(command).ContinueWith<TResult>(t =>
            {
                if (t.IsFaulted)
                {
                    throw t.Exception;
                }
                else
                {
                    return (TResult)(t as Task<object>).Result;
                }
            });
        }

        protected virtual Task SendAsync(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            var commandState = BuildMessageState(commandContext, cancellationToken);
            commandState.CancellationToken.Register(OnCancel, commandState);
            _commandStateQueues.GetOrAdd(commandState.MessageID, commandState);
            Send(commandContext);
            return commandState.TaskCompletionSource.Task;
        }

        protected void OnCancel(object state)
        {
            var messageState = state as IFramework.Message.MessageState;
            if (messageState != null)
            {
                _commandStateQueues.TryRemove(messageState.MessageID);
            }
        }

        public void Send(IEnumerable<IMessageContext> commandContexts)
        {
            commandContexts.ForEach(commandContext => Send(commandContext));
        }
    }

}
