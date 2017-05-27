using System;
using System.IO;
using System.Text;
using Kafka.Client.Exceptions;
using Kafka.Client.Serialization;
using Kafka.Client.Utils;

namespace Kafka.Client.Messages
{
    /// <summary>
    ///     Message send to Kafka server
    /// </summary>
    /// <remarks>
    ///     Format:
    ///     1 byte "magic" identifier to allow format changes
    ///     4 byte CRC32 of the payload
    ///     N - 5 byte payload
    /// </remarks>
    public class Message : IWritable
    {
        public const int DefaultHeaderSize = DefaultMagicLength + DefaultCrcLength + DefaultAttributesLength +
                                             DefaultKeySizeLength + DefaultValueSizeLength;

        private const byte DefaultMagicValue = 0;


        /// <summary>
        ///     Need set magic to 1 while compress,
        ///     See https://cwiki.apache.org/confluence/display/KAFKA/Wire+Format for detail
        /// </summary>
        private const byte MagicValueWhenCompress = 1;

        private const byte DefaultMagicLength = 1;
        private const byte DefaultCrcLength = 4;
        private const byte MagicOffset = DefaultCrcLength;
        private const byte DefaultAttributesLength = 1;
        private const byte DefaultKeySizeLength = 4;
        private const byte DefaultValueSizeLength = 4;
        private const byte CompressionCodeMask = 3;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Message" /> class.
        /// </summary>
        /// <param name="payload">
        ///     The payload.
        /// </param>
        /// <remarks>
        ///     Initializes the magic number as default and the checksum as null. It will be automatically computed.
        /// </remarks>
        public Message(byte[] payload)
            : this(payload, null, CompressionCodecs.NoCompressionCodec)
        {
            Guard.NotNull(payload, "payload");
        }

        public Message(byte[] payload, CompressionCodecs compressionCodec)
            : this(payload, null, compressionCodec)
        {
            Guard.NotNull(payload, "payload");
        }

        /// <summary>
        ///     Initializes a new instance of the Message class.
        /// </summary>
        /// <param name="payload">The data for the payload.</param>
        /// <param name="magic">The magic identifier.</param>
        /// <param name="checksum">The checksum for the payload.</param>
        public Message(byte[] payload, byte[] key, CompressionCodecs compressionCodec)
        {
            Guard.NotNull(payload, "payload");

            var length = DefaultHeaderSize + payload.Length;
            Key = key;
            if (key != null)
                length += key.Length;

            Payload = payload;
            Magic = DefaultMagicValue;
            if (compressionCodec != CompressionCodecs.NoCompressionCodec)
                Attributes |=
                    (byte) (CompressionCodeMask & Messages.CompressionCodec.GetCompressionCodecValue(compressionCodec));

            Size = length;
        }

        /// <summary>
        ///     Gets the payload.
        /// </summary>
        public byte[] Payload { get; }

        /// <summary>
        ///     Gets the magic bytes.
        /// </summary>
        public byte Magic { get; private set; }

        /// <summary>
        ///     Gets the Attributes for the message.
        /// </summary>
        public byte Attributes { get; private set; }

        /// <summary>
        ///     Gets the total size of message.
        /// </summary>
        public int Size { get; }

        public byte[] Key { get; }

        public long Offset { get; set; } = -1;

        /// <summary>
        ///     When produce data, do not need set this field.
        ///     When consume data, need set this field.
        /// </summary>
        public int? PartitionId { get; set; }

        public int KeyLength
        {
            get
            {
                if (Key == null)
                    return -1;
                return Key.Length;
            }
        }

        /// <summary>
        ///     Gets the payload size.
        /// </summary>
        public int PayloadSize => Payload.Length;

        public CompressionCodecs CompressionCodec => Messages.CompressionCodec.GetCompressionCodec(
            Attributes & CompressionCodeMask);

        /// <summary>
        ///     Writes message data into given message buffer
        /// </summary>
        /// <param name="output">
        ///     The output.
        /// </param>
        public void WriteTo(MemoryStream output)
        {
            Guard.NotNull(output, "output");

            using (var writer = new KafkaBinaryWriter(output))
            {
                WriteTo(writer);
            }
        }

        /// <summary>
        ///     Writes message data using given writer
        /// </summary>
        /// <param name="writer">
        ///     The writer.
        /// </param>
        /// <param name="getBuffer"></param>
        public void WriteTo(KafkaBinaryWriter writer)
        {
            Guard.NotNull(writer, "writer");
            writer.Seek(MagicOffset, SeekOrigin.Current);
            var beginningPosition = writer.CurrentPos;
            writer.Write(Magic);
            writer.Write(Attributes);
            writer.Write(KeyLength);
            if (KeyLength != -1)
                writer.Write(Key);
            writer.Write(Payload.Length);
            writer.Write(Payload);
            var crc = ComputeChecksum(writer.Buffer, (int) beginningPosition, Size - MagicOffset);
            writer.Seek(-Size, SeekOrigin.Current);
            writer.Write(crc);
            writer.Seek(Size - DefaultCrcLength, SeekOrigin.Current);
        }

        /// <summary>
        ///     Try to show the payload as decoded to UTF-8.
        /// </summary>
        /// <returns>The decoded payload as string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Magic: ");
            sb.Append(Magic);
            if (Magic == 1)
            {
                sb.Append(", Attributes: ");
                sb.Append(Attributes);
            }

            sb.Append(", topic: ");
            try
            {
                sb.Append(Encoding.UTF8.GetString(Payload));
            }
            catch (Exception)
            {
                sb.Append("n/a");
            }

            return sb.ToString();
        }

        /**
        * A message. The format of an N byte message is the following:
        *
        * 1. 4 byte CRC32 of the message
        * 2. 1 byte "magic" identifier to allow format changes, value is 2 currently
        * 3. 1 byte "attributes" identifier to allow annotations on the message independent of the version (e.g. compression enabled, type of codec used)
        * 4. 4 byte key length, containing length K
        * 5. K byte key
        * 6. 4 byte payload length, containing length V
        * 7. V byte payload
        *
        */
        internal static Message ParseFrom(KafkaBinaryReader reader, long offset, int size, int partitionID)
        {
            Message result;
            var readed = 0;
            var checksum = reader.ReadUInt32();
            readed += 4;
            var magic = reader.ReadByte();
            readed++;

            byte[] payload;
            if (magic == 2 || magic == 0) // some producers (CLI) send magic 0 while others have value of 2
            {
                var attributes = reader.ReadByte();
                readed++;
                var keyLength = reader.ReadInt32();
                readed += 4;
                byte[] key = null;
                if (keyLength != -1)
                {
                    key = reader.ReadBytes(keyLength);
                    readed += keyLength;
                }
                var payloadSize = reader.ReadInt32();
                readed += 4;
                payload = reader.ReadBytes(payloadSize);
                readed += payloadSize;
                result = new Message(payload, key,
                    Messages.CompressionCodec.GetCompressionCodec(attributes & CompressionCodeMask))
                {
                    Offset = offset,
                    PartitionId = partitionID
                };
            }
            else
            {
                payload = reader.ReadBytes(size - DefaultHeaderSize);
                readed += size - DefaultHeaderSize;
                result = new Message(payload) {Offset = offset, PartitionId = partitionID};
            }

            if (size != readed)
                throw new KafkaException(ErrorMapping.InvalidFetchSizeCode);

            return result;
        }

        /// <summary>
        ///     Clean up attributes for message, otherwise there is double decompress at kafka broker side.
        /// </summary>
        internal void CleanMagicAndAttributesBeforeCompress()
        {
            Attributes = 0;
            Magic = DefaultMagicValue;
        }

        /// <summary>
        ///     Restore the Magic and Attributes after compress.
        /// </summary>
        /// <param name="magic"></param>
        /// <param name="attributes"></param>
        internal void RestoreMagicAndAttributesAfterCompress(byte magic, byte attributes)
        {
            Attributes = attributes;
            Magic = magic;
        }

        private uint ComputeChecksum(byte[] message, int offset, int count)
        {
            return Crc32Hasher.ComputeCrcUint32(message, offset, count);
        }
    }
}