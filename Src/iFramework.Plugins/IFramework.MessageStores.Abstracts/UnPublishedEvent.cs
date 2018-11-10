using IFramework.Message;

namespace IFramework.MessageStores.Abstracts
{
    public class UnPublishedEvent : UnSentMessage
    {
        public UnPublishedEvent() { }

        public UnPublishedEvent(IMessageContext messageContext) :
            base(messageContext) { }
    }
}