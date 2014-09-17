using IFramework.Command;
using IFramework.Message;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.MessageQueue.ZeroMQ.MessageFormat;

namespace IFramework.MessageQueue.ZeroMQ
{
    public class DistributableCommandBus : CommandBus, ICommandBus
    {
        readonly IInProcMessageConsumer _commandConsumer;
        readonly IInProcMessageConsumer _commandDistributor;

        readonly bool _isDistributor;

        public DistributableCommandBus(ICommandHandlerProvider handlerProvider,
                          ILinearCommandManager linearCommandManager,
                          IMessageConsumer commandConsumer,
                          string receiveEndPoint,
                          bool inProc)
            : base(handlerProvider, linearCommandManager, receiveEndPoint, inProc)
        {
            _commandConsumer = commandConsumer as IInProcMessageConsumer;
            _commandDistributor = _commandConsumer;
            _isDistributor = _commandDistributor is IMessageDistributor;
        }


        protected override void ConsumeMessage(IMessageReply reply)
        {
            base.ConsumeMessage(reply);
            if (_isDistributor)
            {
                _commandDistributor.EnqueueMessage(new MessageHandledNotification(reply.MessageID).GetFrame());
            }
        }

        protected override Task SendAsync(IMessageContext commandContext, CancellationToken cancellationToken)
        {
            MessageState commandState = BuildMessageState(commandContext, cancellationToken);
            commandState.CancellationToken.Register(OnCancel, commandState);
            CommandStateQueue.Add(commandState.MessageID, commandState);
            Task.Factory.StartNew(() => {
                _commandConsumer.EnqueueMessage(commandContext.GetFrame());
                _Logger.InfoFormat("send to distributor/consumer commandID:{0} payload:{1}",
                                         commandContext.MessageID, commandContext.ToJson());
            });
            return commandState.TaskCompletionSource.Task;
        }

        public new void Start()
        {
            (this as MessageConsumer<IMessageReply>).Start();
        }
    }
}
