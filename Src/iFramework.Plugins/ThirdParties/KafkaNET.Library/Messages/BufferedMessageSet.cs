using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Consumers;
using Kafka.Client.Exceptions;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Messages
{
    /// <summary>
    ///     A collection of messages stored as memory stream
    /// </summary>
    public class BufferedMessageSet : MessageSet, IEnumerable<MessageAndOffset>, IEnumerator<MessageAndOffset>
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(BufferedMessageSet));
        private long currValidBytes;
        private long initialOffset;
        private bool innerDone = true;
        private IEnumerator<MessageAndOffset> innerIter;
        private long lastMessageSize;
        private MessageAndOffset nextItem;
        private ConsumerIteratorState state = ConsumerIteratorState.NotReady;

        private int topIterPosition;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BufferedMessageSet" /> class.
        /// </summary>
        /// <param name="messages">The list of messages.</param>
        public BufferedMessageSet(IEnumerable<Message> messages, int partition)
            : this(messages, (short) ErrorMapping.NoError, partition) { }

        public BufferedMessageSet(IEnumerable<Message> messages, short error, int partition)
            : this(messages, error, 0, partition) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BufferedMessageSet" /> class.
        /// </summary>
        /// <param name="messages">
        ///     The list of messages.
        /// </param>
        /// <param name="errorCode">
        ///     The error code.
        /// </param>
        public BufferedMessageSet(IEnumerable<Message> messages, short errorCode, long initialOffset, int partition)
        {
            var length = GetMessageSetSize(messages);
            Messages = messages;
            ErrorCode = errorCode;
            topIterPosition = 0;
            this.initialOffset = initialOffset;
            currValidBytes = initialOffset;
            PartitionId = partition;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BufferedMessageSet" /> class with compression.
        /// </summary>
        /// <param name="compressionCodec"></param>
        /// <param name="messages">messages to add</param>
        public BufferedMessageSet(CompressionCodecs compressionCodec, IEnumerable<Message> messages, int partition)
        {
            PartitionId = partition;
            IEnumerable<Message> messagesToAdd;
            switch (compressionCodec)
            {
                case CompressionCodecs.NoCompressionCodec:
                    messagesToAdd = messages;
                    break;
                default:
                    var message = CompressionUtils.Compress(messages, compressionCodec, partition);
                    messagesToAdd = new List<Message> {message};
                    break;
            }

            var length = GetMessageSetSize(messagesToAdd);
            Messages = messagesToAdd;
            ErrorCode = (short) ErrorMapping.NoError;
            topIterPosition = 0;
        }

        public int PartitionId { get; }

        /// <summary>
        ///     Gets the error code
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        ///     Gets or sets the offset marking the end of this partition log.
        /// </summary>
        public long HighwaterOffset { get; internal set; }

        /// <summary>
        ///     Gets the list of messages.
        /// </summary>
        public sealed override IEnumerable<Message> Messages { get; protected set; }

        /// <summary>
        ///     Gets the total set size.
        /// </summary>
        public override int SetSize => GetMessageSetSize(Messages);


        public IEnumerator<MessageAndOffset> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public MessageAndOffset Current
        {
            get
            {
                if (state != ConsumerIteratorState.Ready && state != ConsumerIteratorState.Done)
                {
                    throw new NoSuchElementException();
                }

                if (nextItem != null)
                {
                    return nextItem;
                }

                throw new IllegalStateException("Expected item but none found.");
            }
        }

        object IEnumerator.Current => Current;


        public bool MoveNext()
        {
            if (state == ConsumerIteratorState.Failed)
            {
                throw new IllegalStateException("Iterator is in failed state");
            }

            switch (state)
            {
                case ConsumerIteratorState.Done:
                    return false;
                default:
                    return MaybeComputeNext();
            }
        }

        public void Reset()
        {
            topIterPosition = 0;
            currValidBytes = initialOffset;
            lastMessageSize = 0;
            innerIter = null;
            innerDone = true;
        }

        public void Dispose() { }

        public static BufferedMessageSet ParseFrom(KafkaBinaryReader reader, int size, int partitionID)
        {
            var bytesLeft = size;
            if (bytesLeft == 0)
            {
                return new BufferedMessageSet(Enumerable.Empty<Message>(), partitionID);
            }

            var messages = new List<Message>();

            do
            {
                // Already read last message
                if (bytesLeft < 12)
                {
                    break;
                }

                var offset = reader.ReadInt64();
                var msgSize = reader.ReadInt32();
                bytesLeft -= 12;

                if (msgSize > bytesLeft || msgSize < 0)
                {
                    break;
                }

                var msg = Message.ParseFrom(reader, offset, msgSize, partitionID);
                bytesLeft -= msgSize;
                messages.Add(msg);
            } while (bytesLeft > 0);

            if (bytesLeft > 0)
            {
                reader.ReadBytes(bytesLeft);
            }

            return new BufferedMessageSet(messages, partitionID);
        }

        /// <summary>
        ///     Writes content into given stream
        /// </summary>
        /// <param name="output">
        ///     The output stream.
        /// </param>
        public sealed override void WriteTo(MemoryStream output)
        {
            Guard.NotNull(output, "output");
            using (var writer = new KafkaBinaryWriter(output))
            {
                WriteTo(writer);
            }
        }

        /// <summary>
        ///     Writes content into given writer
        /// </summary>
        /// <param name="writer">
        ///     The writer.
        /// </param>
        public sealed override void WriteTo(KafkaBinaryWriter writer)
        {
            Guard.NotNull(writer, "writer");
            foreach (var message in Messages)
            {
                writer.Write(initialOffset++);
                writer.Write(message.Size);
                message.WriteTo(writer);
            }
        }

        /// <summary>
        ///     Gets string representation of set
        /// </summary>
        /// <returns>
        ///     String representation of set
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            var i = 1;
            foreach (var message in Messages)
            {
                sb.Append("Message ");
                sb.Append(i);
                sb.Append(" {Length: ");
                sb.Append(message.Size);
                sb.Append(", ");
                sb.Append(message);
                sb.AppendLine("} ");
                i++;
            }

            return sb.ToString();
        }

        private bool MaybeComputeNext()
        {
            state = ConsumerIteratorState.Failed;
            nextItem = MakeNext();
            if (state == ConsumerIteratorState.Done)
            {
                return false;
            }
            state = ConsumerIteratorState.Ready;
            return true;
        }

        private MessageAndOffset MakeNextOuter()
        {
            if (topIterPosition >= Messages.Count())
            {
                return AllDone();
            }

            var newMessage = Messages.ElementAt(topIterPosition);
            lastMessageSize = newMessage.Size;
            topIterPosition++;
            switch (newMessage.CompressionCodec)
            {
                case CompressionCodecs.NoCompressionCodec:
                    Logger.DebugFormat(
                                       "Message is uncompressed. Valid byte count = {0}",
                                       currValidBytes);
                    innerIter = null;
                    innerDone = true;
                    currValidBytes += 4 + newMessage.Size;
                    return new MessageAndOffset(newMessage, currValidBytes);
                default:
                    Logger.DebugFormat("Message is compressed. Valid byte count = {0}", currValidBytes);
                    innerIter = CompressionUtils.Decompress(newMessage, PartitionId).GetEnumerator();
                    innerDone = !innerIter.MoveNext();
                    return MakeNext();
            }
        }

        private MessageAndOffset MakeNext()
        {
            Logger.DebugFormat("MakeNext() in deepIterator: innerDone = {0}", innerDone);

            switch (innerDone)
            {
                case true:
                    return MakeNextOuter();
                default:
                    var messageAndOffset = innerIter.Current;
                    innerDone = !innerIter.MoveNext();
                    if (innerDone)
                    {
                        currValidBytes += 4 + lastMessageSize;
                    }

                    return new MessageAndOffset(messageAndOffset.Message, currValidBytes);
            }
        }

        private MessageAndOffset AllDone()
        {
            state = ConsumerIteratorState.Done;
            return null;
        }
    }
}