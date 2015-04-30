using IFramework.Command;
using IFramework.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeroMQ;
using IFramework.Infrastructure;
using System.Threading;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.UnitOfWork;
using IFramework.Config;
using System.Collections.Concurrent;
using IFramework.SysExceptions;
using System.Data;
using IFramework.MessageQueue.ZeroMQ.MessageFormat;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class CommandBus : MessageConsumer<IMessageReply>, ICommandBus
    {
        protected ICommandHandlerProvider HandlerProvider { get; set; }
        protected ILinearCommandManager LinearCommandManager { get; set; }

        protected BlockingCollection<IMessageContext> _toBeSentCommandQueue;
        protected Hashtable CommandStateQueue { get; set; }
        private Task _sendCommandWorkTask;

        protected IMessageStore MessageStore
        {
            get
            {
                return IoCFactory.Resolve<IMessageStore>();
            }
        }

        protected bool InProc { get; set; }
        protected List<ZmqSocket> CommandSenders { get; set; }

        public CommandBus(ICommandHandlerProvider handlerProvider,
                          ILinearCommandManager linearCommandManager,
                          string receiveEndPoint,
                          bool inProc)
            : base(receiveEndPoint)
        {
            CommandStateQueue = Hashtable.Synchronized(new Hashtable());
            HandlerProvider = handlerProvider;
            LinearCommandManager = linearCommandManager;
            InProc = inProc;
        }

        public CommandBus(ICommandHandlerProvider handlerProvider,
                          ILinearCommandManager linearCommandManager,
                          string receiveEndPoint,
                          bool inProc,
                          string[] targetEndPoints)
            : this(handlerProvider, linearCommandManager, receiveEndPoint, inProc)
        {
            _toBeSentCommandQueue = new BlockingCollection<IMessageContext>();
            CommandSenders = new List<ZmqSocket>();

            targetEndPoints.ForEach(targetEndPoint =>
            {
                try
                {
                    var commandSender = ZeroMessageQueue.ZmqContext.CreateSocket(SocketType.PUSH);
                    commandSender.Connect(targetEndPoint);
                    CommandSenders.Add(commandSender);
                }
                catch (Exception ex)
                {
                    _Logger.Error(ex.GetBaseException().Message, ex);
                }
            });
        }

        public override void Start()
        {
            _sendCommandWorkTask = Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var messageStore = IoCFactory.Resolve<IMessageStore>())
                    {
                        messageStore.GetAllUnSentCommands()
                            .ForEach(commandContext => _toBeSentCommandQueue.Add(commandContext));
                    }
                    while (!_Exit)
                    {
                        try
                        {
                            var commandContext = _toBeSentCommandQueue.Take();
                            SendCommand(commandContext);
                            Task.Factory.StartNew(() =>
                            {
                                using (var messageStore = IoCFactory.Resolve<IMessageStore>())
                                {
                                    messageStore.RemoveSentCommand(commandContext.MessageID);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            _Logger.Debug("send command quit", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _Logger.Error("start send command work failed", ex);
                }
                

            }, TaskCreationOptions.LongRunning);


            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
            if (_sendCommandWorkTask != null)
            {
                _toBeSentCommandQueue.CompleteAdding();
                if (_sendCommandWorkTask.Wait(5000))
                {
                    _sendCommandWorkTask.Dispose();
                }
                else
                {
                    _Logger.ErrorFormat("SendCommandWorkTask can't be stopped!");
                }
            }
        }

        public void Add(ICommand command)
        {
            var currentMessageContext = PerMessageContextLifetimeManager.CurrentMessageContext;
            if (currentMessageContext == null)
            {
                throw new CurrentMessageContextIsNull();
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
            IMessageContext commandContext = new MessageContext(command, ReceiveEndPoint, commandKey);
            ((MessageContext)currentMessageContext).ToBeSentMessageContexts.Add(commandContext);
        }

        protected virtual void SendCommand(IMessageContext messageContext)
        {
            try
            {
                var frame = messageContext.GetFrame();
                ZmqSocket commandSender = null;

                if (CommandSenders.Count == 1)
                {
                    commandSender = CommandSenders[0];
                }
                else if (CommandSenders.Count > 1)
                {
                    var commandKey = messageContext.Key;
                    int keyHashCode = !string.IsNullOrWhiteSpace(commandKey) ?
                        commandKey.GetUniqueCode() : messageContext.MessageID.GetUniqueCode();
                    commandSender = CommandSenders[keyHashCode % CommandSenders.Count];
                }
                if (commandSender == null) return;
                var status = commandSender.SendFrame(frame);
                _Logger.InfoFormat("send commandID:{0} length:{1} send status:{2}",
                    messageContext.MessageID, frame.BufferSize, status.ToString());
            }
            catch (Exception ex)
            {
                _Logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        protected override void ConsumeMessage(IMessageReply reply)
        {
            _Logger.InfoFormat("Handle reply:{0} content:{1}", reply.MessageID, reply.ToJson()); 
            var messageState = CommandStateQueue[reply.MessageID] as IFramework.Message.MessageState;
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

        protected override void ReceiveMessage(Frame frame)
        {
            var reply = frame.GetMessage<MessageReply>();
            if (reply != null)
            {
                MessageQueue.Add(reply);
            }
        }

        protected MessageState BuildMessageState(IMessageContext messageContext, CancellationToken cancellationToken)
        {
            var pendingRequestsCts = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource
                   .CreateLinkedTokenSource(cancellationToken, pendingRequestsCts.Token);
            cancellationToken = linkedCts.Token;
            var source = new TaskCompletionSource<object>();
            var state = new MessageState
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
                var linearKey = LinearCommandManager.GetLinearKey(command as ILinearCommand);
                if (linearKey != null)
                {
                    commandKey = linearKey.ToString();
                }
            }
            IMessageContext commandContext = new MessageContext(command, ReceiveEndPoint, commandKey);

            Task task = null;
            if (InProc && currentMessageContext == null)
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
                    IMessageStore messageStore = null;
                    try
                    {
                        var needRetry = command.NeedRetry;
                        object result = null;
                        PerMessageContextLifetimeManager.CurrentMessageContext = commandContext;
                        messageStore = IoCFactory.Resolve<IMessageStore>();
                        if (!messageStore.HasCommandHandled(commandContext.MessageID))
                        {
                            var commandHandler = HandlerProvider.GetHandler(command.GetType());
                            if (commandHandler == null)
                            {
                                PerMessageContextLifetimeManager.CurrentMessageContext = null;
                                throw new NoHandlerExists();
                            }

                            do
                            {
                                try
                                {
                                    ((dynamic)commandHandler).Handle((dynamic)command);
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
                            return result;
                        }
                        else
                        {
                            throw new MessageDuplicatelyHandled();
                        }
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
                }, cancellationToken);
            }
            return task;
        }

        protected virtual Task SendAsync(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            var commandState = BuildMessageState(commandContext, cancellationToken);
            commandState.CancellationToken.Register(OnCancel, commandState);
            CommandStateQueue.Add(commandState.MessageID, commandState);
            _toBeSentCommandQueue.Add(commandContext, cancellationToken);
            return commandState.TaskCompletionSource.Task;
        }

        protected void OnCancel(object state)
        {
            var messageState = state as MessageState;
            if (messageState != null)
            {
                CommandStateQueue.TryRemove(messageState.MessageID);
            }
        }

        public void Send(IEnumerable<IMessageContext> commandContexts)
        {
            commandContexts.ForEach(commandContext => _toBeSentCommandQueue.Add(commandContext));
        }
    }
}
