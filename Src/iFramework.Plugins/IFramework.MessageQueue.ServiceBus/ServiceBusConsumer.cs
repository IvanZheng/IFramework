using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.MessageQueue.ServiceBus
{
    public abstract class ServiceBusConsumer : ICommitOffsetable
    {
        protected Task _consumerTask;
        protected CancellationTokenSource _cancellationTokenSource;
        protected ILogger _logger;
        protected OnMessagesReceived _onMessagesReceived;
        public string Id
        {
            get; protected set;
        }

        public ServiceBusConsumer(string id, OnMessagesReceived onMessagesReceived)
        {
            Id = id;
            _onMessagesReceived = onMessagesReceived;
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
        }

        public abstract void CommitOffset(IMessageContext messageContext);

        public abstract void Start();

        public void Stop()
        {
            _cancellationTokenSource?.Cancel(true);
            _consumerTask?.Wait();
        }
    }
}
