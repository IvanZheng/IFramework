using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.Command
{
    public interface ICommandBus
    {
        void Start();
        void Stop();
        Task<MessageResponse> StartSaga(ICommand command, string sagaId = null);
        Task<MessageResponse> StartSaga(ICommand command, TimeSpan timeout, string sagaId = null);

        Task<MessageResponse> StartSaga(ICommand command,
                                        CancellationToken sendCancellationToken,
                                        TimeSpan sendTimeout,
                                        CancellationToken replyCancellationToken,
                                        string sagaId = null);

        Task<MessageResponse> SendAsync(ICommand command, bool needReply = false);
        Task<MessageResponse> SendAsync(ICommand command, TimeSpan timeout, bool needReply = false);

        Task<MessageResponse> SendAsync(ICommand command,
                                        CancellationToken sendCancellationToken,
                                        TimeSpan sendTimeout,
                                        CancellationToken replyCancellationToken,
                                        bool needReply = false);

        void SendMessageStates(IEnumerable<MessageState> messageStates);

        IMessageContext WrapCommand(ICommand command,
                                    bool needReply = false,
                                    SagaInfo sagaInfo = null,
                                    string producer = null);
    }
}