using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using IFramework.MessageQueue;

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
            using (var scope = ObjectProviderFactory.Instance.ObjectProvider.CreateScope())
            using (var messageStore = scope.GetService<IMessageStore>())
            {
                return messageStore.GetAllUnPublishedEvents((messageId, message, topic, correlationId, replyEndPoint, sagaInfo, producer) =>
                                                                MessageQueueClient.WrapMessage(message, key: message.Key,
                                                                                                topic: topic, messageId: messageId,
                                                                                                correlationId: correlationId,
                                                                                                replyEndPoint: replyEndPoint,
                                                                                                sagaInfo: sagaInfo, producer: producer));
            }
        }

        protected override async Task SendMessageStateAsync(MessageState messageState, CancellationToken cancellationToken)
        {
            var messageContext = messageState.MessageContext;
            await MessageQueueClient.PublishAsync(messageContext, messageContext.Topic ?? DefaultTopic, cancellationToken);
            CompleteSendingMessage(messageState);
        }

        protected override void CompleteSendingMessage(MessageState messageState)
        {
            messageState.SendTaskCompletionSource?
                .TrySetResult(new MessageResponse(messageState.MessageContext,
                                                  null,
                                                  false));
            if (NeedMessageStore)
            {
                ObjectProviderFactory.Instance
                                     .ObjectProvider
                                     .GetService<IMessageStoreDaemon>()
                                     .RemovePublishedEvent(messageState.MessageID);
            }
        }
    }
}