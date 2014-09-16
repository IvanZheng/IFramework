using IFramework.Command;
using IFramework.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Message.Impl;
using System.Threading;
using IFramework.SysExceptions;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.UnitOfWork;
using IFramework.Config;
using System.Collections.Concurrent;
using IFramework.MessageQueue.EQueue.MessageFormat;
using EQueueClientsProducers = EQueue.Clients.Producers;
using EQueueClientsConsumers = EQueue.Clients.Consumers;
using EQueueProtocols = EQueue.Protocols;
using System.Data;

namespace IFramework.MessageQueue.EQueue
{
    public class CommandBus : MessageConsumer<MessageReply>, ICommandBus
    {
        protected ICommandHandlerProvider HandlerProvider { get; set; }
        protected ILinearCommandManager LinearCommandManager { get; set; }
        protected Hashtable CommandStateQueue { get; set; }
        protected string CommandTopic { get; set; }
        protected string ReplyTopic { get; set; }
        protected EQueueClientsProducers.ProducerSetting ProducerSetting { get; set; }
        protected string ProducerName { get; set; }
        protected EQueueClientsProducers.Producer Producer { get; set; }
        protected Task SubscriptionConsumeTask;
        protected Task SendCommandWorkTask;
        private BlockingCollection<MessageContext> ToBeSentCommandQueue;

        IMessageStore _MessageStore;
        protected IMessageStore MessageStore
        {
            get
            {
                return _MessageStore ?? (_MessageStore = IoCFactory.Resolve<IMessageStore>());
            }
        }

        protected bool InProc { get; set; }

        public CommandBus(string name, ICommandHandlerProvider handlerProvider,
                          ILinearCommandManager linearCommandManager,
                          string brokerAddress,
                          int producerBrokerPort,
                          EQueueClientsConsumers.ConsumerSetting consumerSetting,
                          string groupName,
                          string replyTopic,
                          string commandTopic,
                          bool inProc)
            : base(name, consumerSetting, groupName, replyTopic)
        {
            CommandStateQueue = Hashtable.Synchronized(new Hashtable());
            ToBeSentCommandQueue = new BlockingCollection<MessageContext>();
            HandlerProvider = handlerProvider;
            LinearCommandManager = linearCommandManager;
            CommandTopic = commandTopic;
            ReplyTopic = replyTopic;
            InProc = inProc;
            ProducerSetting = new EQueueClientsProducers.ProducerSetting();
            ProducerSetting.BrokerAddress = brokerAddress;
            ProducerSetting.BrokerPort = producerBrokerPort;
            ProducerName = name;
       }

        public void Start()
        {
            #region init sending commands Worker

            #region Init  Command Queue client

            Producer = new EQueueClientsProducers.Producer(string.Format("{0}-Reply-Producer", ProducerName), ProducerSetting);
     
            #endregion

            SendCommandWorkTask = Task.Factory.StartNew(() =>
            {
                using (var messageStore = IoCFactory.Resolve<IMessageStore>())
                {
                    messageStore.GetAllUnSentCommands()
                        .ForEach(commandContext => ToBeSentCommandQueue.Add(commandContext as MessageContext));
                }
                while (!_exit)
                {
                    try
                    {
                        var commandContext = ToBeSentCommandQueue.Take();
                        SendCommand(commandContext);
                    }
                    catch (Exception ex)
                    {
                        _Logger.Debug("send command quit", ex);
                    }
                }
            }, TaskCreationOptions.LongRunning);
            #endregion

            #region init process command reply worker

            base.Start();

            #endregion
        }

        internal void SendCommands(IEnumerable<IMessageContext> commandContexts)
        {
            commandContexts.ForEach(commandContext => ToBeSentCommandQueue.Add(commandContext as MessageContext));
        }

        private void SendCommand(MessageContext commandContext)
        {
            Producer.SendAsync(commandContext.EQueueMessage, commandContext.Key)
                .ContinueWith(task =>
                {
                    if (task.Result.SendStatus == EQueueClientsProducers.SendStatus.Success)
                    {
                        using (var messageStore = IoCFactory.Resolve<IMessageStore>())
                        {
                            messageStore.RemoveSentCommand(commandContext.MessageID);
                        }
                        _Logger.DebugFormat("sent commandID {0}, body: {1}", commandContext.MessageID,
                                                                        commandContext.Message.ToJson());
                    }
                    else
                    {
                        _Logger.ErrorFormat("Send Command {0}", task.Result.SendStatus.ToString());
                    }
                });
        }

        protected override void ConsumeMessage(MessageReply reply, EQueueProtocols.QueueMessage queueMessage)
        {
            _Logger.DebugFormat("Handle reply:{0} content:{1}", reply.MessageID, reply.ToJson());
            var messageState = CommandStateQueue[reply.MessageID] as MessageState;
            if (messageState != null)
            {
                CommandStateQueue.TryRemove(reply.MessageID);
                if (reply.Result is Exception)
                {
                    messageState.TaskCompletionSource.TrySetException(reply.Result as Exception);
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
                var linearKey = LinearCommandManager.GetLinearKey(command as ILinearCommand);
                if (linearKey != null)
                {
                    commandKey = linearKey.ToString();
                }
            }
            IMessageContext commandContext = null;
            commandContext = new MessageContext(CommandTopic, command, ReplyTopic, commandKey);
            Task task = null;
            if (InProc && currentMessageContext == null && !(command is ILinearCommand))
            {
                task = SendInProc(commandContext, cancellationToken);
            }
            else
            {
                if (currentMessageContext != null)
                {
                    ((MessageContext)currentMessageContext).ToBeSentMessageContexts.Add(commandContext);
                }
                else
                {
                    task = SendAsync(commandContext, cancellationToken);
                }
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
                task = Task.Factory.StartNew(() =>
                {
                    var needRetry = command.NeedRetry;
                    object result = null;
                    PerMessageContextLifetimeManager.CurrentMessageContext = commandContext;
                    IMessageStore messageStore = IoCFactory.Resolve<IMessageStore>();

                    var commandHandler = HandlerProvider.GetHandler(command.GetType());
                    if (commandHandler == null)
                    {
                        PerMessageContextLifetimeManager.CurrentMessageContext = null;
                        throw new NoHandlerExists();
                    }
                    try
                    {
                        //var unitOfWork = IoCFactory.Resolve<IUnitOfWork>();
                        do
                        {
                            try
                            {
                                ((dynamic)commandHandler).Handle((dynamic)command);
                                //unitOfWork.Commit();
                                result = commandContext.Reply;
                                needRetry = false;
                            }
                            catch (Exception ex)
                            {
                                if (!(ex is OptimisticConcurrencyException) || !needRetry)
                                {
                                    throw;
                                }
                            }
                        } while (needRetry);
                    }
                    catch (Exception e)
                    {
                        if (e is DomainException)
                        {
                            _Logger.Warn(command.ToJson(), e);
                        }
                        else
                        {
                            _Logger.Error(command.ToJson(), e);
                        }
                        if (messageStore != null)
                        {
                            messageStore.SaveFailedCommand(commandContext);
                        }
                        throw;
                    }
                    finally
                    {
                        PerMessageContextLifetimeManager.CurrentMessageContext = null;
                    }
                    return result;
                }, cancellationToken);
            }
            return task;
        }

        protected virtual Task SendAsync(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            var commandState = BuildMessageState(commandContext, cancellationToken);
            commandState.CancellationToken.Register(OnCancel, commandState);
            CommandStateQueue.Add(commandState.MessageID, commandState);
            ToBeSentCommandQueue.Add(commandContext as MessageContext, cancellationToken);
            return commandState.TaskCompletionSource.Task;
        }

        protected void OnCancel(object state)
        {
            var messageState = state as IFramework.Message.MessageState;
            if (messageState != null)
            {
                CommandStateQueue.TryRemove(messageState.MessageID);
            }
        }
    }
}
