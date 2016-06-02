using IFramework.Infrastructure;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IFramework.Message
{

    public class MessageResponse
    {
        private bool _needReply;
        private Task<object> _replyTask;
        public IMessageContext MessageContext { get; set; }
        public MessageResponse(IMessageContext messageContext)
            : this(messageContext, null, false)
        {
            
        }

        public MessageResponse(IMessageContext messageConext, Task<object> replayTask, bool needReply)
        {
            MessageContext = messageConext;
            _replyTask = replayTask;
            _needReply = needReply;
        }


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
            var result = await Reply;
            return (TResult)result;
        }

        public async Task<TResult> ReadAsAsync<TResult>(TimeSpan timeout)
        {
            var result =  await Reply.Timeout(timeout);
            return (TResult)result;
        }
    }

  
}
