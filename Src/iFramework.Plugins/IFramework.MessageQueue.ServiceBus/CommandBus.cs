using IFramework.Command;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Message;
using IFramework.MessageQueue.MessageFormat;
using IFramework.SysExceptions;
using IFramework.UnitOfWork;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.ServiceBus
{
    public class CommandBus : MessageProcessor, ICommandBus
    {
        protected ICommandHandlerProvider _handlerProvider;
        protected ILinearCommandManager _linearCommandManager;
        protected Hashtable _commandStateQueues;
        protected Task _subscriptionConsumeTask;
        protected string _replyTopicName;
        protected string _replySubscriptionName;
        protected string[] _commandQueueNames;
        protected List<QueueClient> _commandQueueClients;
        protected SubscriptionClient _replySubscriptionClient;
        protected IMessageStore MessageStore
        {
            get
            {
                return IoCFactory.Resolve<IMessageStore>();
            }
        }

        protected bool InProc { get; set; }
        protected List<QueueClient> CommandProducers { get; set; }

        public CommandBus(ICommandHandlerProvider handlerProvider,
                          ILinearCommandManager linearCommandManager,
                          string serviceBusConnectionString,
                          string[] commandQueueNames,
                          string replyTopicName,
                          string replySubscriptionName,
                          bool inProc)
            : base(serviceBusConnectionString)
        {
            _commandStateQueues = Hashtable.Synchronized(new Hashtable());
            _handlerProvider = handlerProvider;
            _linearCommandManager = linearCommandManager;
            _replyTopicName = replyTopicName;
            _replySubscriptionName = replySubscriptionName;
            _commandQueueNames = commandQueueNames;
            _commandQueueClients = new List<QueueClient>();
            InProc = inProc;
        }



        public void Start()
        {
            if (_commandQueueNames != null && _commandQueueNames.Length > 0)
            {
                _commandQueueNames.ForEach(commandQueueName => 
                    _commandQueueClients.Add(CreateQueueClient(commandQueueName)));
            }

            _replySubscriptionClient = CreateSubscriptionClient(_replyTopicName, _replySubscriptionName);
            _subscriptionConsumeTask = Task.Factory.StartNew(() =>
            {
                while (!_exit)
                {
                    BrokeredMessage brokeredMessage = null;
                    try
                    {
                        brokeredMessage = _replySubscriptionClient.Receive();
                        var reply = brokeredMessage.GetBody<string>().ToJsonObject<MessageReply>();
                        ConsumeReply(reply);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug("consume reply error", ex);
                    }
                    finally
                    {
                        if (brokeredMessage != null)
                        {
                            brokeredMessage.Complete();
                        }
                    }
                }

            }, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            _exit = true;
            if (_subscriptionConsumeTask != null)
            {
                _replySubscriptionClient.Close();
                if (!_subscriptionConsumeTask.Wait(1000))
                {
                    _logger.ErrorFormat("receiver can't be stopped!");
                }
            }
        }
        protected virtual void SendCommand(IFramework.Message.MessageState commandState)
        {
            try
            {
                QueueClient commandProducer = null;
                if (_commandQueueClients.Count == 1)
                {
                    commandProducer = _commandQueueClients[0];
                }
                else if (_commandQueueClients.Count > 1)
                {
                    var commandKey = commandState.MessageContext.Key;
                    int keyHashCode = !string.IsNullOrWhiteSpace(commandKey) ?
                        commandKey.GetHashCode() : commandState.MessageID.GetHashCode();
                    commandProducer = _commandQueueClients[keyHashCode % _commandQueueClients.Count];
                }
                if (commandProducer == null) return;
                var brokeredMessage = new BrokeredMessage(commandState.MessageContext.ToJson());
                commandProducer.Send(brokeredMessage);
                _logger.InfoFormat("send commandID:{0} length:{1} send status:{2}",
                    commandState.MessageID, brokeredMessage.Size, brokeredMessage.State);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        protected void ConsumeReply(IMessageReply reply)
        {
            _logger.InfoFormat("Handle reply:{0} content:{1}", reply.MessageID, reply.ToJson());
            var messageState = _commandStateQueues[reply.MessageID] as IFramework.Message.MessageState;
            if (messageState != null)
            {
                _commandStateQueues.TryRemove(reply.MessageID);
                if (reply.Exception != null)
                {
                    messageState.TaskCompletionSource.TrySetException(reply.Exception);
                }
                else
                {
                    messageState.TaskCompletionSource.TrySetResult(reply.Result);
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
        public Task Send(ICommand command, CancellationToken cancellationToken)
        {
            var currentMessageContext = PerMessageContextLifetimeManager.CurrentMessageContext;
            if (currentMessageContext != null && currentMessageContext.Message is ICommand)
            {
                // A command sent in a CommandContext is not allowed. We throw exception!!!
                throw new NotSupportedException("Command is not allowd to be sent in another command context!");
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
            var commandContext = new MessageContext(command, _replyTopicName, commandKey);
            Task task;
            if (InProc && currentMessageContext == null)
            {
                task = SendInProc(commandContext, cancellationToken);
            }
            else
            {
                if (currentMessageContext != null && Configuration.IsPersistanceMessage)
                {
                    MessageStore.Save(commandContext, currentMessageContext.MessageID);
                }
                task = SendAsync(commandContext, cancellationToken);
            }
            return task;
        }

        protected virtual Task SendInProc(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            Task task = null;
            var command = commandContext.Message as ICommand;
            if (command is ILinearCommand)
            {
                task = SendAsync(commandContext, cancellationToken);
            }
            else if (command != null) //if not a linear command, we run synchronously.
            {
                task = new Task<object>(() =>
                {
                    var needRetry = command.NeedRetry;
                    object result = null;
                    do
                    {
                        PerMessageContextLifetimeManager.CurrentMessageContext = commandContext;
                        var commandHandler = _handlerProvider.GetHandler(command.GetType());
                        if (commandHandler == null)
                        {
                            PerMessageContextLifetimeManager.CurrentMessageContext = null;
                            throw new NoHandlerExists();
                        }

                        try
                        {
                            var unitOfWork = IoCFactory.Resolve<IUnitOfWork>();
                            ((dynamic)commandHandler).Handle((dynamic)command);
                            unitOfWork.Commit();
                            result = commandContext.Reply;
                            needRetry = false;
                        }
                        catch (Exception e)
                        {
                            if (!(e is OptimisticConcurrencyException) || !needRetry)
                            {
                                if (e is DomainException)
                                {
                                    _logger.Warn(command.ToJson(), e);
                                }
                                else
                                {
                                    _logger.Error(command.ToJson(), e);
                                }
                                throw;
                            }
                        }
                        finally
                        {
                            PerMessageContextLifetimeManager.CurrentMessageContext = null;
                        }
                    } while (needRetry);
                    return result;
                });
                task.RunSynchronously();
            }
            return task;
        }

        protected virtual Task SendAsync(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            var commandState = BuildMessageState(commandContext, cancellationToken);
            commandState.CancellationToken.Register(OnCancel, commandState);
            _commandStateQueues.Add(commandState.MessageID, commandState);
            SendCommand(commandState);
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
    }
}
