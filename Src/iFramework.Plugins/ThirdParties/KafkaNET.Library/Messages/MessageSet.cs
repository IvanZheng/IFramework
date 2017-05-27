using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Messages
{
    /// <summary>
    ///     A set of messages. A message set has a fixed serialized form, though the container
    ///     for the bytes could be either in-memory or on disk.
    /// </summary>
    /// <remarks>
    ///     Format:
    ///     8 byte long offset
    ///     4 byte size containing an integer N
    ///     N message bytes as described in the message class
    /// </remarks>
    public abstract class MessageSet : IWritable
    {
        protected const byte DefaultMessageLengthSize = 4 + 8;

        /// <summary>
        ///     Gets the total size of this message set in bytes
        /// </summary>
        public abstract int SetSize { get; }

        public abstract IEnumerable<Message> Messages { get; protected set; }

        /// <summary>
        ///     Writes content into given stream
        /// </summary>
        /// <param name="output">
        ///     The output stream.
        /// </param>
        public abstract void WriteTo(MemoryStream output);

        /// <summary>
        ///     Writes content into given writer
        /// </summary>
        /// <param name="writer">
        ///     The writer.
        /// </param>
        public abstract void WriteTo(KafkaBinaryWriter writer);

        /// <summary>
        ///     Gives the size of a size-delimited entry in a message set
        /// </summary>
        /// <param name="message">
        ///     The message.
        /// </param>
        /// <returns>
        ///     Size of message
        /// </returns>
        public static int GetEntrySize(Message message)
        {
            Guard.NotNull(message, "message");

            return message.Size + DefaultMessageLengthSize;
        }

        /// <summary>
        ///     Gives the size of a list of messages
        /// </summary>
        /// <param name="messages">
        ///     The messages.
        /// </param>
        /// <returns>
        ///     Size of all messages
        /// </returns>
        public static int GetMessageSetSize(IEnumerable<Message> messages)
        {
            return messages == null ? 0 : messages.Sum(x => GetEntrySize(x));
        }
    }
}