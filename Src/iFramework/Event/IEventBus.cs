using System.Collections.Generic;
using IFramework.Bus;
using IFramework.Command;

namespace IFramework.Event
{
    public interface IEventBus : IBus<IEvent>
    {
        void SendCommand(ICommand command);
        void PublishAnyway(params IEvent[] events);
        IEnumerable<ICommand> GetCommands();
        IEnumerable<IEvent> GetEvents();
        IEnumerable<object> GetSagaResults();
        IEnumerable<IEvent> GetToPublishAnywayMessages();
        void FinishSaga(object sagaResult);
        void ClearMessages();
    }
}