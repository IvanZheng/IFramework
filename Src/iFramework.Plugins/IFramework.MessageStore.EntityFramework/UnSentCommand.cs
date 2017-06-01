using IFramework.Message;

namespace IFramework.MessageStoring
{
    public class UnSentCommand : UnSentMessage
    {
        public UnSentCommand() { }

        public UnSentCommand(IMessageContext messageContext) :
            base(messageContext) { }
    }
}