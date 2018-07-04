using IFramework.Message;

namespace IFramework.MessageStores.Relational
{
    public class UnSentCommand : UnSentMessage
    {
        public UnSentCommand() { }

        public UnSentCommand(IMessageContext messageContext) :
            base(messageContext) { }
    }
}