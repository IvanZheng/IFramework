using System.Globalization;
using Kafka.Client.Exceptions;

namespace Kafka.Client.Messages
{
    public static class CompressionCodec
    {
        public static CompressionCodecs GetCompressionCodec(int codec)
        {
            switch (codec)
            {
                case 0:
                    return CompressionCodecs.NoCompressionCodec;
                case 1:
                    return CompressionCodecs.GZIPCompressionCodec;
                case 2:
                    return CompressionCodecs.SnappyCompressionCodec;
                default:
                    throw new UnknownCodecException(string.Format(
                                                                  CultureInfo.CurrentCulture,
                                                                  "{0} is an unknown compression codec",
                                                                  codec));
            }
        }

        public static byte GetCompressionCodecValue(CompressionCodecs compressionCodec)
        {
            switch (compressionCodec)
            {
                case CompressionCodecs.SnappyCompressionCodec:
                    return 2;
                case CompressionCodecs.DefaultCompressionCodec:
                case CompressionCodecs.GZIPCompressionCodec:
                    return 1;
                case CompressionCodecs.NoCompressionCodec:
                    return 0;
                default:
                    throw new UnknownCodecException(string.Format(
                                                                  CultureInfo.CurrentCulture,
                                                                  "{0} is an unknown compression codec",
                                                                  compressionCodec));
            }
        }
    }
}