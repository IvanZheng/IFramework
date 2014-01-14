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

namespace IFramework.MessageQueue.ZeroMQ
{
    public class CommandBus : MessageConsumer<IMessageReply>, ICommandBus
    {
        ICommandHandlerProvider HandlerProvider { get; set; }
        ILinearCommandManager LinearCommandManager { get; set; }
        BlockingCollection<MessageState> CommandQueue { get; set; }
        Hashtable MessageStateQueue { get; set; }
        protected IMessageStore MessageStore
        {
            get
            {
                return IoCFactory.Resolve<IMessageStore>();
            }
        }
        bool InProc { get; set; }
        List<ZmqSocket> CommandSenders { get; set; }

        public CommandBus(ICommandHandlerProvider handlerProvider,
                          ILinearCommandManager linerCommandManager,
                          string receiveEndPoint,
                          bool inProc,
                          string[] targetEndPoints)
            : base(receiveEndPoint)
        {
            HandlerProvider = handlerProvider;

            MessageStateQueue = Hashtable.Synchronized(new Hashtable());
            CommandQueue = new BlockingCollection<MessageState>();

            LinearCommandManager = linerCommandManager;
            InProc = inProc;
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
                    System.Diagnostics.Debug.WriteLine(ex.GetBaseException().Message);
                }
            });
        }

        public override void Start()
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

        private void SendCommand(MessageState commandState)
        {
            try
            {
                MessageStateQueue.Add(commandState.MessageID, commandState);
                var frame = commandState.MessageContext.GetFrame();
                ZmqSocket commandSender = null;

                if (CommandSenders.Count == 1)
                {
                    commandSender = CommandSenders[0];
                }
                else if (CommandSenders.Count > 1)
                {
                    var command = commandState.MessageContext.Message;
                    var keyHashCode = 0;
                    if (command is ILinearCommand)
                    {
                        keyHashCode = LinearCommandManager.GetLinearKey(command as ILinearCommand).GetHashCode();
                    }
                    else
                    {
                        keyHashCode = command.GetType().Name.GetHashCode();
                    }
                    commandSender = CommandSenders[keyHashCode % CommandSenders.Count];
                }
                if (commandSender != null)
                {
                    var status = commandSender.SendFrame(frame);
                    System.Diagnostics.Debug.WriteLine(string.Format("commandID:{0} length:{1} send status:{2}",
                           commandState.MessageID, frame.BufferSize, status.ToString()));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.GetBaseException().Message);
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
                    var resultProperty = messageState.MessageContext.Message.GetType().GetProperty("Result");
                    if (resultProperty != null)
                    {
                        messageState.MessageContext.Message.SetValueByKey("Result", reply.Result);
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

        MessageState BuildMessageState(IMessageContext messageContext, CancellationToken cancellationToken)
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

            var commandContext = new MessageContext(command, this.ReceiveEndPoint);
            System.Diagnostics.Debug.WriteLine(
                string.Format("CommandBus:CommandID :{0}", commandContext.MessageID));

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

        Task SendInProc(IMessageContext commandContext, CancellationToken cancellationToken)
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

        Task SendAsync(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            var command = commandContext.Message as ICommand;
            MessageState messageState = BuildMessageState(commandContext, cancellationToken);
            messageState.CancellationToken.Register(onCancel, messageState);

            CommandQueue.Add(messageState);

            return messageState.TaskCompletionSource.Task;
        }

        private void onCancel(object state)
        {
            var messageState = state as MessageState;
            MessageStateQueue.TryRemove(messageState.MessageID);
        }
    }
}
