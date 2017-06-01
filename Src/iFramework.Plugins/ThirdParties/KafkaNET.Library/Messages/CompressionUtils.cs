using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Kafka.Client.Exceptions;
using Kafka.Client.Messages.Compression;
using Kafka.Client.Serialization;

namespace Kafka.Client.Messages
{
    public static class CompressionUtils
    {
        public static ILogger Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(CompressionUtils));

        public static Message Compress(IEnumerable<Message> messages, int partition)
        {
            return Compress(messages, CompressionCodecs.DefaultCompressionCodec, partition);
        }

        public static Message Compress(IEnumerable<Message> messages, CompressionCodecs compressionCodec, int partition)
        {
            switch (compressionCodec)
            {
                case CompressionCodecs.DefaultCompressionCodec:
                case CompressionCodecs.GZIPCompressionCodec:
                    using (var outputStream = new MemoryStream())
                    {
                        using (var gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
                        {
                            //if (Logger.IsDebugEnabled)
                            {
                                Logger.DebugFormat(
                                                   "Allocating BufferedMessageSet of size = {0}",
                                                   MessageSet.GetMessageSetSize(messages));
                            }

                            var bufferedMessageSet = new BufferedMessageSet(messages, partition);
                            using (var inputStream = new MemoryStream(bufferedMessageSet.SetSize))
                            {
                                bufferedMessageSet.WriteTo(inputStream);
                                inputStream.Position = 0;
                                try
                                {
                                    gZipStream.Write(inputStream.ToArray(), 0, inputStream.ToArray().Length);
                                    gZipStream.Close();
                                }
                                catch (IOException ex)
                                {
                                    Logger.ErrorFormat("Error while writing to the GZIP stream {0}",
                                                       ex.FormatException());
                                    throw;
                                }
                            }

                            var oneCompressedMessage = new Message(outputStream.ToArray(), compressionCodec)
                            {
                                PartitionId = partition
                            };
                            return oneCompressedMessage;
                        }
                    }

                case CompressionCodecs.SnappyCompressionCodec:
                    Logger.DebugFormat(
                                       "Allocating BufferedMessageSet of size = {0}",
                                       MessageSet.GetMessageSetSize(messages));

                    var messageSet = new BufferedMessageSet(messages, partition);
                    using (var inputStream = new MemoryStream(messageSet.SetSize))
                    {
                        messageSet.WriteTo(inputStream);
                        inputStream.Position = 0;

                        try
                        {
                            return new Message(SnappyHelper.Compress(inputStream.GetBuffer()), compressionCodec)
                            {
                                PartitionId = partition
                            };
                        }
                        catch (Exception ex)
                        {
                            Logger.ErrorFormat("Error while writing to the Snappy stream {0}", ex.FormatException());
                            throw;
                        }
                    }

                default:
                    throw new UnknownCodecException(string.Format(CultureInfo.CurrentCulture, "Unknown Codec: {0}",
                                                                  compressionCodec));
            }
        }

        public static BufferedMessageSet Decompress(Message message, int partition)
        {
            switch (message.CompressionCodec)
            {
                case CompressionCodecs.DefaultCompressionCodec:
                case CompressionCodecs.GZIPCompressionCodec:
                    var inputBytes = message.Payload;
                    using (var outputStream = new MemoryStream())
                    {
                        using (var inputStream = new MemoryStream(inputBytes))
                        {
                            using (var gzipInputStream = new GZipStream(inputStream, CompressionMode.Decompress))
                            {
                                try
                                {
                                    gzipInputStream.CopyTo(outputStream);
                                    gzipInputStream.Close();
                                }
                                catch (IOException ex)
                                {
                                    Logger.InfoFormat("Error while reading from the GZIP input stream: {0}",
                                                      ex.FormatException());
                                    throw;
                                }
                            }
                        }

                        outputStream.Position = 0;
                        using (var reader = new KafkaBinaryReader(outputStream))
                        {
                            return BufferedMessageSet.ParseFrom(reader, (int) outputStream.Length, partition);
                        }
                    }

                case CompressionCodecs.SnappyCompressionCodec:
                    try
                    {
                        using (var stream = new MemoryStream(SnappyHelper.Decompress(message.Payload)))
                        {
                            using (var reader = new KafkaBinaryReader(stream))
                            {
                                return BufferedMessageSet.ParseFrom(reader, (int) stream.Length, partition);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorFormat("Error while reading from the Snappy input stream  {0}",
                                           ex.FormatException());
                        throw;
                    }

                default:
                    throw new UnknownCodecException(string.Format(CultureInfo.CurrentCulture, "Unknown Codec: {0}",
                                                                  message.CompressionCodec));
            }
        }
    }
}