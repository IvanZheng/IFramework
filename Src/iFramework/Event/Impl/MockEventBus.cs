using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Command;

namespace IFramework.Event.Impl
{
    public class MockEventBus : IEventBus
    {
        public void Commit()
        {

        }

        public void Dispose()
        {

        }

        public void Publish<TMessage>(TMessage @event) where TMessage : IEvent
        {

        }

        public void Publish<TMessage>(IEnumerable<TMessage> events) where TMessage : IEvent
        {
        }

        public IEnumerable<IEvent> GetEvents()
        {
            return null;
        }


        public void ClearMessages()
        {
        }

        public void PublishAnyway(params IEvent[] events)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEvent> GetToPublishAnywayMessages()
        {
            throw new NotImplementedException();
        }

        public void SendCommand(ICommand command)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ICommand> GetCommands()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object> GetSagaResults()
        {
            throw new NotImplementedException();
        }

        public void FinishSaga(object sagaResult)
        {
            throw new NotImplementedException();
        }
    }
}
