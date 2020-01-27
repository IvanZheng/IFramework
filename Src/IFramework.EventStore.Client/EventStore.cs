using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using IFramework.Command;
using IFramework.Event;
using IFramework.Message;

namespace IFramework.EventStore.Client
{
    public class EventStore : IEventStore
    {
        private readonly IEventStoreConnection _connection;
        private readonly IEventDeserializer _eventDeserializer;
        private readonly IEventSerializer _eventSerializer;
        private readonly IMessageTypeProvider _messageTypeProvider;

        public EventStore(IEventStoreConnection connection,
                          IEventSerializer eventSerializer,
                          IEventDeserializer eventDeserializer,
                          IMessageTypeProvider messageTypeProvider)
        {
            _connection = connection;
            _eventSerializer = eventSerializer;
            _eventDeserializer = eventDeserializer;
            _messageTypeProvider = messageTypeProvider;
        }

        public Task Connect()
        {
            return _connection.ConnectAsync();
        }

        public async Task<IEvent[]> GetEvents(string id, long start = 0, long? end = null)
        {
            var streamEvents = new List<ResolvedEvent>();

            StreamEventsSlice currentSlice;
            var nextSliceStart = start;
            do
            {
                var count = end.HasValue ? (int) Math.Min(200, end.Value - nextSliceStart) : 200;
                currentSlice = await _connection.ReadStreamEventsForwardAsync(id,
                                                                              nextSliceStart,
                                                                              count,
                                                                              false)
                                                .ConfigureAwait(false);

                nextSliceStart = currentSlice.NextEventNumber;

                streamEvents.AddRange(currentSlice.Events);
                if (end.HasValue && currentSlice.LastEventNumber >= end)
                {
                    break;
                }
            } while (!currentSlice.IsEndOfStream);

            return streamEvents.Select(se =>
                               {
                                   var messageType = _messageTypeProvider.GetMessageType(se.Event.EventType);
                                   return _eventDeserializer.Deserialize(se.Event.Data, messageType) as IEvent;
                               })
                               .ToArray();
        }

        public Task AppendEvents(string id, long expectedVersion, string correlationId, object result, params IEvent[] events)
        {
            var targetVersion = expectedVersion;
            var eventStream = events.Select(e =>
                                    {
                                        var messageCode = _messageTypeProvider.GetMessageCode(e.GetType());
                                        return new EventData(Guid.NewGuid(),
                                                             messageCode,
                                                             true,
                                                             _eventSerializer.Serialize(e),
                                                             _eventSerializer.Serialize(new EventMetadata(e,
                                                                                                          ++targetVersion,
                                                                                                          messageCode,
                                                                                                          correlationId)));
                                    })
                                    .ToArray();
            return _connection.AppendToStreamAsync(id, expectedVersion, eventStream);
        }

        public Task<IEvent[]> GetEvents(string id, string commandId)
        {
            throw new NotImplementedException();
        }

        public Task<(ICommand[], IEvent[])> HandleEvent(string subscriber, string eventId, ICommand[] commands, IEvent[] events)
        {
            throw new NotImplementedException();
        }
    }
}