using IFramework.Message;

namespace IFramework.MessageStores.Abstracts
{
    public class UnSentCommand : UnSentMessage
    {
        public UnSentCommand() { }

        public UnSentCommand(IMessageContext messageContext) :
            base(messageContext) { }
    }
}