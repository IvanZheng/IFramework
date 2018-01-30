using System;
using System.Collections.Generic;
using IFramework.Command;

namespace IFramework.Event.Impl
{
    public class MockEventBus : IEventBus
    {
        public void Dispose() { }

        public void Publish(IEvent @event){ }

        public void Publish(IEnumerable<IEvent> events){ }

        public IEnumerable<IEvent> GetEvents()
        {
            return null;
        }

        public void ClearMessages() { }

        public void SendCommand(ICommand command)
        {
        }

        public void PublishAnyway(params IEvent[] events)
        {
        }

        public IEnumerable<ICommand> GetCommands()
        {
            return null;
        }

        public IEnumerable<object> GetSagaResults()
        {
            return null;
        }

        public IEnumerable<IEvent> GetToPublishAnywayMessages()
        {
            return null;
        }

        public void FinishSaga(object sagaResult)
        {
        }

        public void Publish<TMessage>(TMessage @event) where TMessage : IEvent
        {
        }

        public void Publish<TMessage>(IEnumerable<TMessage> events) where TMessage : IEvent
        {
        }
    }
}