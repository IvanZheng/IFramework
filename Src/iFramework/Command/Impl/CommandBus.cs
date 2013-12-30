using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.SysException;
using IFramework.UnitOfWork;
using IFramework.Message.Impl;
using System.Collections;
using Microsoft.Practices.Unity;
using System.Threading;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Config;

namespace IFramework.Command.Impl
{
    public class CommandBus : ICommandBus
    {
        ICommandHandlerProvider HandlerProvider { get; set; }
        IMessageConsumer CommandConsumer { get; set; }
        Hashtable MessageStateQueue { get; set; }
        protected IMessageStore MessageStore
        {
            get
            {
                return IoCFactory.Resolve<IMessageStore>();
            }
        }

        bool InProc { get; set; }
        public CommandBus(ICommandHandlerProvider handlerProvider,
                          IMessageConsumer commandConsumer,
                          bool inProc)
        {
            HandlerProvider = handlerProvider;
            CommandConsumer = commandConsumer;
            if (CommandConsumer != null)
            {
                CommandConsumer.MessageHandled += CommandConsumer_MessageHandled;
            }
            MessageStateQueue = Hashtable.Synchronized(new Hashtable());
            InProc = inProc;
        }

        void CommandConsumer_MessageHandled(MessageReply reply)
        {
            var messageState = MessageStateQueue[reply.MessageID] as MessageState;
            if (messageState != null)
            {
                MessageStateQueue.TryRemove(reply.MessageID);
                object result = null;
                if (reply.Exception != null)
                {
                    messageState.TaskCompletionSource.TrySetException(reply.Exception);
                }
                else
                {
                    var resultProperty = messageState.Message.GetType().GetProperty("Result");
                    if (resultProperty != null)
                    {
                        messageState.Message.SetValueByKey("Result", reply.Result);
                    }
                    messageState.TaskCompletionSource.TrySetResult(result);
                }
            }
        }

        MessageState BuildMessageState(string messageID, ICommand command, CancellationToken cancellationToken)
        {
            var pendingRequestsCts = new CancellationTokenSource();
            CancellationTokenSource linkedCts = CancellationTokenSource
                   .CreateLinkedTokenSource(cancellationToken, pendingRequestsCts.Token);
            cancellationToken = linkedCts.Token;
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();
            var state = new MessageState
            {
                MessageID = messageID,
                TaskCompletionSource = source,
                CancellationToken = cancellationToken,
                Message = command
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

            var commandContext = new MessageContext(command); 
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
                        throw new NoCommandHandlerExists();
                    }
                    
                    MessageState messageState = BuildMessageState(commandContext.MessageID, command, cancellationToken);
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
            MessageState messageState = BuildMessageState(commandContext.MessageID, command, cancellationToken);
            messageState.CancellationToken.Register(onCancel, messageState);
            Task.Factory.StartNew(() =>
            {
                MessageStateQueue.Add(commandContext.MessageID, messageState);
                CommandConsumer.PushMessageContext(commandContext);
            });
            return messageState.TaskCompletionSource.Task;
        }

        private void onCancel(object state)
        {
            var messageState = state as MessageState;
            MessageStateQueue.TryRemove(messageState.MessageID);
        }
    }
}
