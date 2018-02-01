using IFramework.Message;

namespace IFramework.MessageStores.Sqlserver
{
    public class UnSentCommand : UnSentMessage
    {
        public UnSentCommand() { }

        public UnSentCommand(IMessageContext messageContext) :
            base(messageContext) { }
    }
}