using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.Command.Impl
{
    public class MockCommandBus : ICommandBus
    {
        public void SendMessageStates(IEnumerable<MessageState> messageStates)
        {

        }

        public Task<MessageResponse> SendAsync(ICommand command, bool needReply = true)
        {
            throw new NotImplementedException();
        }

        public Task<MessageResponse> SendAsync(ICommand command, CancellationToken sendCancellationToken, TimeSpan sendTimeout, CancellationToken replyCancellationToken, bool needReply = true)
        {
            throw new NotImplementedException();
        }
        public Task<MessageResponse> SendAsync(ICommand command, TimeSpan timeout, bool needReply = true)
        {
            throw new NotImplementedException();
        }
        public void Start()
        {

        }

        public void Stop()
        {

        }

        public IMessageContext WrapCommand(ICommand command, bool needReply = false, SagaInfo sagaInfo = null)
        {
            throw new NotImplementedException();
        }

        public Task<MessageResponse> StartSaga(ICommand command, string sagaId = null)
        {
            throw new NotImplementedException();
        }

        public Task<MessageResponse> StartSaga(ICommand command, TimeSpan timeout, string sagaId = null)
        {
            throw new NotImplementedException();
        }

        public Task<MessageResponse> StartSaga(ICommand command, CancellationToken sendCancellationToken, TimeSpan sendTimeout, CancellationToken replyCancellationToken, string sagaId = null)
        {
            throw new NotImplementedException();
        }
    }
}
