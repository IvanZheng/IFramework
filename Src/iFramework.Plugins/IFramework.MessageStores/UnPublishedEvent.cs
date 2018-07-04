using IFramework.Message;

namespace IFramework.MessageStores.Relational
{
    public class UnPublishedEvent : UnSentMessage
    {
        public UnPublishedEvent() { }

        public UnPublishedEvent(IMessageContext messageContext) :
            base(messageContext) { }
    }
}