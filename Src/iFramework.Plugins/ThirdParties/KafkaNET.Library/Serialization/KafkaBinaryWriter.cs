using System;
using System.IO;
using System.Net;
using System.Text;
using Kafka.Client.Requests;

namespace Kafka.Client.Serialization
{
    /// <summary>
    ///     Writes data into underlying stream using big endian bytes order for primitive types
    ///     and UTF-8 encoding for strings.
    /// </summary>
    public class KafkaBinaryWriter : BinaryWriter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaBinaryWriter" /> class
        ///     using big endian bytes order for primive types and UTF-8 encoding for strings.
        /// </summary>
        protected KafkaBinaryWriter() { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaBinaryWriter" /> class
        ///     using big endian bytes order for primive types and UTF-8 encoding for strings.
        /// </summary>
        /// <param name="output">
        ///     The output stream.
        /// </param>
        public KafkaBinaryWriter(MemoryStream output)
            : base(output) { }

        public byte[] Buffer => ((MemoryStream) OutStream).GetBuffer();

        public long CurrentPos => OutStream.Position;

        /// <summary>
        ///     Flushes data into stream and resets position pointer.
        /// </summary>
        /// <param name="disposing">
        ///     Not used
        /// </param>
        protected override void Dispose(bool disposing)
        {
            Flush();
            OutStream.Position = 0;
        }

        /// <summary>
        ///     Writes four-bytes signed integer to the current stream using big endian bytes order
        ///     and advances the stream position by four bytes
        /// </summary>
        /// <param name="value">
        ///     The value to write.
        /// </param>
        public override void Write(int value)
        {
            var bigOrdered = IPAddress.HostToNetworkOrder(value);
            base.Write(bigOrdered);
        }

        [CLSCompliant(false)]
        public override void Write(uint value)
        {
            var bigOrdered = IPAddress.HostToNetworkOrder((int) value);
            base.Write(bigOrdered);
        }

        /// <summary>
        ///     Writes eight-bytes signed integer to the current stream using big endian bytes order
        ///     and advances the stream position by eight bytes
        /// </summary>
        /// <param name="value">
        ///     The value to write.
        /// </param>
        public override void Write(long value)
        {
            var bigOrdered = IPAddress.HostToNetworkOrder(value);
            base.Write(bigOrdered);
        }

        /// <summary>
        ///     Writes two-bytes signed integer to the current stream using big endian bytes order
        ///     and advances the stream position by two bytes
        /// </summary>
        /// <param name="value">
        ///     The value to write.
        /// </param>
        public override void Write(short value)
        {
            var bigOrdered = IPAddress.HostToNetworkOrder(value);
            base.Write(bigOrdered);
        }

        /// <summary>
        ///     Writes topic and his size into underlying stream using given encoding.
        /// </summary>
        /// <param name="topic">
        ///     The topic to write.
        /// </param>
        /// <param name="encoding">
        ///     The encoding to use.
        /// </param>
        public void WriteShortString(string text, string encoding = AbstractRequest.DefaultEncoding)
        {
            if (string.IsNullOrEmpty(text))
            {
                short defaultValue = -1;
                Write(defaultValue);
            }
            else
            {
                var length = (short) text.Length;
                Write(length);
                var encoder = Encoding.GetEncoding(encoding);
                var encodedTopic = encoder.GetBytes(text);
                Write(encodedTopic);
            }
        }
    }
}