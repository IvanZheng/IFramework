using IFramework.Bus;
using IFramework.Command;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
