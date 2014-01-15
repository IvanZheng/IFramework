using IFramework.Command;
using IFramework.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;
using IFramework.Infrastructure;
using IFramework.Message.Impl;
using System.Threading;
using IFramework.SysException;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.UnitOfWork;
using IFramework.Config;
using System.Collections.Concurrent;
using IFramework.MessageQueue.MessageFormat;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class CommandBus : MessageConsumer<IMessageReply>, ICommandBus
    {
        protected ICommandHandlerProvider HandlerProvider { get; set; }
        protected ILinearCommandManager LinearCommandManager { get; set; }
        protected BlockingCollection<MessageState> CommandQueue { get; set; }
        protected Hashtable MessageStateQueue { get; set; }
        
        IMessageStore _MessageStore;
        protected IMessageStore MessageStore
        {
            get
            {
                return _MessageStore ??  (_MessageStore  = IoCFactory.Resolve<IMessageStore>());
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

        public new void Start()
        {
            base.Start();

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var commandState = CommandQueue.Take();
                    SendCommand(commandState);
                }
            });
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
                    var keyHashCode = 0;
                    if (!string.IsNullOrWhiteSpace(commandKey))
                    {
                        keyHashCode = commandKey.GetHashCode();
                    }
                    else
                    {
                        keyHashCode = commandState.MessageID.GetHashCode();
                    }
                    commandSender = CommandSenders[keyHashCode % CommandSenders.Count];
                }
                if (commandSender != null)
                {
                    var status = commandSender.SendFrame(frame);
                    _Logger.DebugFormat("commandID:{0} length:{1} send status:{2}",
                                          commandState.MessageID, frame.BufferSize, status.ToString());
                }
            }
            catch (Exception ex)
            {
                _Logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        protected override void ConsumeMessage(IMessageReply reply)
        {
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
                    if (reply.Result != null)
                    {
                        var command = messageState.MessageContext.Message;
                        command.SetValueByKey("Result", reply.Result);
                    }
                    messageState.TaskCompletionSource.TrySetResult(null);
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
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();
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
                commandKey = LinearCommandManager.GetLinearKey(command as ILinearCommand).ToString();
            }
            IMessageContext commandContext = new MessageContext(command, this.ReceiveEndPoint, commandKey);

            Task task = null;
            if (InProc && currentMessageContext == null)
            {
                task = SendInProc(commandContext, cancellationToken);
            }
            else
            {
                task = SendAsync(commandContext, cancellationToken);
                if (currentMessageContext != null && Configuration.GetAppConfig<bool>("PersistanceMessage"))
                {
                    //TODO: persistance command with domain event ID
                    MessageStore.Save(commandContext, currentMessageContext.MessageID);
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
            else //if not a linear command, we run synchronously.
            {
                task = new Task(() =>
                {
                    var commandHandler = HandlerProvider.GetHandler(command.GetType());
                    if (commandHandler == null)
                    {
                        throw new NoHandlerExists();
                    }

                    //MessageState messageState = BuildMessageState(commandContext, cancellationToken);
                    PerMessageContextLifetimeManager.CurrentMessageContext = commandContext;
                    try
                    {
                        var unitOfWork = IoCFactory.Resolve<IUnitOfWork>();
                        commandHandler.Handle(command);
                        unitOfWork.Commit();
                    }
                    finally
                    {
                        commandContext.ClearItems();
                    }
                });
                task.RunSynchronously();
                if (task.Exception != null)
                {
                    throw task.Exception.GetBaseException();
                }
            }
            return task;
        }

        protected virtual Task SendAsync(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            var command = commandContext.Message as ICommand;
            MessageState commandState = BuildMessageState(commandContext, cancellationToken);
            commandState.CancellationToken.Register(onCancel, commandState);
            MessageStateQueue.Add(commandState.MessageID, commandState);
            CommandQueue.Add(commandState);
            return commandState.TaskCompletionSource.Task;
        }

        protected void onCancel(object state)
        {
            var messageState = state as MessageState;
            MessageStateQueue.TryRemove(messageState.MessageID);
        }
    }
}
