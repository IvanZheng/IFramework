using IFramework.Command;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue.MessageFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class DistributableCommandBus : CommandBus, ICommandBus
    {
        IMessageConsumer _CommandConsumer;

        public DistributableCommandBus(ICommandHandlerProvider handlerProvider,
                          ILinearCommandManager linearCommandManager,
                          IMessageConsumer commandConsumer,
                          string receiveEndPoint,
                          bool inProc)
            : base(handlerProvider, linearCommandManager, receiveEndPoint, inProc)
        {
            _CommandConsumer = commandConsumer;
        }


        protected override void ConsumeMessage(IMessageReply reply)
        {
            base.ConsumeMessage(reply);
            var commandDistributer = _CommandConsumer as CommandDistributer;
            if (commandDistributer != null)
            {
                commandDistributer.EnqueueMessage(new MessageHandledNotification(reply.MessageID).GetFrame());
            }
        }

        protected override Task SendAsync(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            var command = commandContext.Message as ICommand;
            MessageState commandState = BuildMessageState(commandContext, cancellationToken);
            commandState.CancellationToken.Register(onCancel, commandState);
            MessageStateQueue.Add(commandState.MessageID, commandState);
            Task.Factory.StartNew(() => {
                _CommandConsumer.EnqueueMessage(commandContext.GetFrame());
            });
            return commandState.TaskCompletionSource.Task;
        }

        public new void Start()
        {
            (this as MessageConsumer<IMessageReply>).Start();
        }
    }
}
