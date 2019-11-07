using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Event
{
    public interface IEventStore
    {
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
        /// <param name="events"></param>
        /// <returns></returns>
        Task AppendEvents(string id, long expectedVersion, params IEvent[] events);
    }
}
