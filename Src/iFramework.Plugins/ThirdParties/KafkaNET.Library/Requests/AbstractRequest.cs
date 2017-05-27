using System.IO;
using System.Text;

namespace Kafka.Client.Requests
{
    /// <summary>
    ///     Base request to make to Kafka.
    /// </summary>
    public abstract class AbstractRequest
    {
        public const string DefaultEncoding = "UTF-8";
        public const short DefaultTopicLengthIfNonePresent = 2;

        protected const byte DefaultRequestSizeSize = 4;
        protected const byte DefaultRequestIdSize = 2;

        public MemoryStream RequestBuffer { get; protected set; }

        public abstract RequestTypes RequestType { get; }

        protected short RequestTypeId => (short) RequestType;

        protected static short GetTopicLength(string topic, string encoding = DefaultEncoding)
        {
            var encoder = Encoding.GetEncoding(encoding);
            return string.IsNullOrEmpty(topic) ? DefaultTopicLengthIfNonePresent : (short) encoder.GetByteCount(topic);
        }

        protected short GetShortStringLength(string text, string encoding = DefaultEncoding)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }
            var encoder = Encoding.GetEncoding(encoding);
            return (short) encoder.GetByteCount(text);
        }

        public short GetShortStringWriteLength(string text, string encoding = DefaultEncoding)
        {
            return (short) (2 + GetShortStringLength(text, encoding));
        }
    }
}