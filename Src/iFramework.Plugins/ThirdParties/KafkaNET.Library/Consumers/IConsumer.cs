using System;
using Kafka.Client.Requests;
using Kafka.Client.Responses;

namespace Kafka.Client.Consumers
{
    /// <summary>
    ///     The low-level API of consumer of Kafka messages
    /// </summary>
    /// <remarks>
    ///     Maintains a connection to a single broker and has a close correspondence
    ///     to the network requests sent to the server.
    /// </remarks>
    public interface IConsumer : IDisposable
    {
        /// <summary>
        ///     Fetch a set of messages from a topic.
        /// </summary>
        /// <param name="request">
        ///     Specifies the topic name, topic partition, starting byte offset, maximum bytes to be fetched.
        /// </param>
        /// <returns>
        ///     A set of fetched messages.
        /// </returns>
        /// <remarks>
        ///     Offset is passed in on every request, allowing the user to maintain this metadata
        ///     however they choose.
        /// </remarks>
        FetchResponse Fetch(FetchRequest request);

        /// <summary>
        ///     Gets a list of valid offsets (up to maxSize) before the given time.
        /// </summary>
        /// <param name="request">
        ///     The offset request.
        /// </param>
        /// <returns>
        ///     The list of offsets, in descending order.
        /// </returns>
        OffsetResponse GetOffsetsBefore(OffsetRequest request);
    }
}