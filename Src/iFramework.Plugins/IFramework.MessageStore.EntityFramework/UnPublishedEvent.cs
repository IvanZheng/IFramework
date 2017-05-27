using IFramework.Message;

namespace IFramework.MessageStoring
{
    public class UnPublishedEvent : UnSentMessage
    {
        public UnPublishedEvent()
        {
        }

        public UnPublishedEvent(IMessageContext messageContext) :
            base(messageContext)
        {
        }
    }
}