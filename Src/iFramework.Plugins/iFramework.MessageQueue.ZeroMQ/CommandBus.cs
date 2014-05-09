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
using IFramework.MessageQueue.MessageFormat;
using IFramework.SysExceptions;
using System.Data;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class CommandBus : MessageConsumer<IMessageReply>, ICommandBus
    {
        protected ICommandHandlerProvider HandlerProvider { get; set; }
        protected ILinearCommandManager LinearCommandManager { get; set; }
        protected BlockingCollection<MessageState> CommandQueue { get; set; }
        protected Hashtable MessageStateQueue { get; set; }
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
            MessageStateQueue = Hashtable.Synchronized(new Hashtable());
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
            CommandQueue = new BlockingCollection<MessageState>();
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
            base.Start();
            if (CommandQueue != null)
            {
                _sendCommandWorkTask = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        while (true)
                        {
                            var commandState = CommandQueue.Take();
                            SendCommand(commandState);
                        }
                    }
                    catch (Exception ex)
                    {
                        _Logger.Debug("end send command", ex);
                    }

                }, TaskCreationOptions.LongRunning);
            }
        }

        public override void Stop()
        {
            if (_sendCommandWorkTask != null)
            {
                CommandQueue.CompleteAdding();
                if (_sendCommandWorkTask.Wait(2000))
                {
                    _sendCommandWorkTask.Dispose();
                }
                else
                {
                    _Logger.ErrorFormat(" consumer can't be stopped!");
                }
            }
            base.Stop();
        }
        protected virtual void SendCommand(MessageState commandState)
        {
            try
            {
                var frame = commandState.MessageContext.GetFrame();
                ZmqSocket commandSender = null;

                if (CommandSenders.Count == 1)
                {
                    commandSender = CommandSenders[0];
                }
                else if (CommandSenders.Count > 1)
                {
                    var commandKey = commandState.MessageContext.Key;
                    int keyHashCode = !string.IsNullOrWhiteSpace(commandKey) ? 
                        commandKey.GetHashCode() : commandState.MessageID.GetHashCode();
                    commandSender = CommandSenders[keyHashCode % CommandSenders.Count];
                }
                if (commandSender == null) return;
                var status = commandSender.SendFrame(frame);
                _Logger.InfoFormat("send commandID:{0} length:{1} send status:{2}",
                    commandState.MessageID, frame.BufferSize, status.ToString());
            }
            catch (Exception ex)
            {
                _Logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        protected override void ConsumeMessage(IMessageReply reply)
        {
            _Logger.InfoFormat("Handle reply:{0} content:{1}", reply.MessageID, reply.ToJson());
            var messageState = MessageStateQueue[reply.MessageID] as MessageState;
            if (messageState != null)
            {
                MessageStateQueue.TryRemove(reply.MessageID);
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
                        var commandHandler = HandlerProvider.GetHandler(command.GetType());
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
                                    _Logger.Warn(command.ToJson(), e);
                                }
                                else
                                {
                                    _Logger.Error(command.ToJson(), e);
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


                //if (task.Exception != null)
                //{
                //    throw task.Exception.GetBaseException();
                //}
            }
            return task;
        }

        protected virtual Task SendAsync(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            var commandState = BuildMessageState(commandContext, cancellationToken);
            commandState.CancellationToken.Register(OnCancel, commandState);
            MessageStateQueue.Add(commandState.MessageID, commandState);
            CommandQueue.Add(commandState, cancellationToken);
            return commandState.TaskCompletionSource.Task;
        }

        protected void OnCancel(object state)
        {
            var messageState = state as MessageState;
            if (messageState != null)
            {
                MessageStateQueue.TryRemove(messageState.MessageID);
            }
        }
    }
}
