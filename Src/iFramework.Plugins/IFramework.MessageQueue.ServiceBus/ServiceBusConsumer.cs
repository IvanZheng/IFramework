using System.Threading;
using System.Threading.Tasks;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;

namespace IFramework.MessageQueue.ServiceBus
{
    public abstract class ServiceBusConsumer : ICommitOffsetable
    {
        protected CancellationTokenSource _cancellationTokenSource;
        protected Task _consumerTask;
        protected ILogger _logger;
        protected OnMessagesReceived _onMessagesReceived;

        public ServiceBusConsumer(string id, OnMessagesReceived onMessagesReceived)
        {
            Id = id;
            _onMessagesReceived = onMessagesReceived;
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(GetType());
        }

        public string Id { get; protected set; }

        public abstract void CommitOffset(IMessageContext messageContext);

        public abstract void Start();

        public void Stop()
        {
            _cancellationTokenSource?.Cancel(true);
            _consumerTask?.Wait();
        }
    }
}