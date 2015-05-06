using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using IFramework.MessageQueue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Message.Impl
{
    public class MessagePublisher : MessageSender, IMessagePublisher
    {
        public MessagePublisher(IMessageQueueClient messageQueueClient, string defaultTopic)
            : base(messageQueueClient, defaultTopic)
        {
            if (string.IsNullOrEmpty(defaultTopic))
            {
                throw new Exception("message sender must have a default topic.");
            }
        }

        protected override IEnumerable<IMessageContext> GetAllUnSentMessages()
        {
            using (var messageStore = IoCFactory.Resolve<IMessageStore>("perResolveMessageStore"))
            {
                return messageStore.GetAllUnPublishedEvents((messageId, message, topic, correlationID) =>
                                        _messageQueueClient.WrapMessage(message, topic: topic, messageId: messageId, correlationId: correlationID));
            }
        }

        protected override void Send(IMessageContext messageContext, string topic)
        {
            _messageQueueClient.Publish(messageContext, topic);
        }

        protected override void CompleteSendingMessage(string messageId)
        {
            Task.Factory.StartNew(() =>
            {
                using (var messageStore = IoCFactory.Resolve<IMessageStore>("perResolveMessageStore"))
                {
                    messageStore.RemovePublishedEvent(messageId);
                }
            });
        }
    }
}
