using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IFramework.Command;

namespace IFramework.Event
{
    public interface IEventStore
    {
        Task Connect();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="start"></param>
        /// <param name="end">if null, read to end</param>
        /// <returns></returns>
        Task<IEvent[]> GetEvents(string id, long start = 0, long? end = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="expectedVersion">The event version that you expect the stream to currently be at.</param>
        /// <param name="correlationId"></param>
        /// <param name="result"></param>
        /// <param name="events"></param>
        /// <returns></returns>
        Task AppendEvents(string id, long expectedVersion, string correlationId, object result, params IEvent[] events);

        /// <summary>
        /// get aggregate's events by commandId
        /// </summary>
        /// <param name="id">aggregate id</param>
        /// <param name="commandId">command id</param>
        /// <returns></returns>
        Task<IEvent[]> GetEvents(string id, string commandId);

        /// <summary>
        /// return commands that subscriber generated in the event handling,
        /// if event has been handled before, return the last handling commands ignoring the current commands
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="eventId"></param>
        /// <param name="commands"></param>
        /// <param name="events"></param>
        /// <returns></returns>
        Task<(ICommand[], IEvent[])> HandleEvent(string subscriber, string eventId, ICommand[] commands, IEvent[] events);
    }
}
