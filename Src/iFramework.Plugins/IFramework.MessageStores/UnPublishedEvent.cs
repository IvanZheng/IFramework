using IFramework.Message;

namespace IFramework.MessageStores.Sqlserver
{
    public class UnPublishedEvent : UnSentMessage
    {
        public UnPublishedEvent() { }

        public UnPublishedEvent(IMessageContext messageContext) :
            base(messageContext) { }
    }
}