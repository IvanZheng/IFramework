using System.Threading.Tasks;

namespace IFramework.Message
{
    public class MessageState
    {
        public MessageState(IMessageContext messageContext, bool needReply = false) :
            this(messageContext, null, needReply) { }

        public MessageState(IMessageContext messageContext,
                            TaskCompletionSource<MessageResponse> sendTaskCompletionSource,
                            bool needReply) :
            this(messageContext, sendTaskCompletionSource, null, needReply) { }

        public MessageState(IMessageContext messageContext,
                            TaskCompletionSource<MessageResponse> sendTaskCompletionSource,
                            TaskCompletionSource<object> replyTaskCompletionSource,
                            bool needReply)
        {
            MessageContext = messageContext;
            MessageID = messageContext.MessageId;
            NeedReply = needReply;
            SendTaskCompletionSource = sendTaskCompletionSource;
            ReplyTaskCompletionSource = replyTaskCompletionSource;
        }

        public string MessageID { get; set; }
        public bool NeedReply { get; set; }
        public IMessageContext MessageContext { get; set; }
        public TaskCompletionSource<object> ReplyTaskCompletionSource { get; set; }
        public TaskCompletionSource<MessageResponse> SendTaskCompletionSource { get; set; }
    }
}