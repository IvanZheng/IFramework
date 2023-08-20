using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.MessageQueue.Client.Abstracts;

namespace IFramework.MessageQueue.RocketMQ
{
    public class RocketMQConsumer: MessageConsumer
    {
        private readonly string[] _brokerAddress;

        //public delegate void OnRocketMQMessageReceived(RocketMQConsumer consumer, PullResult message, CancellationToken cancellationToken);

        //private MQConsumer _consumer;
        public RocketMQConsumer(string[] brokerAddress, string[] topics, string groupId, string consumerId, ConsumerConfig consumerConfig = null)
            : base(topics, groupId, consumerId, consumerConfig)
        {
            _brokerAddress = brokerAddress;
        }

        public override void Start()
        {
            base.Start();
        }

        protected override void PollMessages(CancellationToken cancellationToken)
        {
            //var consumeResult = _consumer.Pull(cancellationToken);
            //if (consumeResult != null)
            //{
            //    _consumer_OnMessage(_consumer, consumeResult, cancellationToken);
            //}
        }



        public override Task CommitOffsetAsync(string broker, string topic, int partition, long offset)
        {
            throw new NotImplementedException();
        }
    }
}
