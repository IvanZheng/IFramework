using System;
using System.Threading.Tasks;
using IFramework.Infrastructure;

namespace IFramework.Message
{
    public class MessageResponse
    {
        private readonly bool _needReply;
        private readonly Task<object> _replyTask;

        public MessageResponse(IMessageContext messageContext)
            : this(messageContext, null, false) { }

        public MessageResponse(IMessageContext messageContext, Task<object> replayTask, bool needReply)
        {
            MessageContext = messageContext;
            _replyTask = replayTask;
            _needReply = needReply;
        }

        public IMessageContext MessageContext { get; set; }


        public Task<object> Reply
        {
            get
            {
                if (!_needReply)
                {
                    throw new Exception("Current response is set not to need message reply!");
                }
                return _replyTask;
            }
        }

        public async Task<TResult> ReadAsAsync<TResult>()
        {
            var result = await Reply.ConfigureAwait(false);
            return (TResult) result;
        }

        public async Task<TResult> ReadAsAsync<TResult>(TimeSpan timeout)
        {
            var result = await Reply.Timeout(timeout).ConfigureAwait(false);
            return (TResult) result;
        }
    }
}