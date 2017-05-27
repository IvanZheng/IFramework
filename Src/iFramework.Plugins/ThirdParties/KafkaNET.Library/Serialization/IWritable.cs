using System.IO;

namespace Kafka.Client.Serialization
{
    /// <summary>
    ///     Writes content into given stream
    /// </summary>
    internal interface IWritable
    {
        /// <summary>
        ///     Writes content into given stream
        /// </summary>
        /// <param name="output">
        ///     The output stream.
        /// </param>
        void WriteTo(MemoryStream output);

        /// <summary>
        ///     Writes content into given writer
        /// </summary>
        /// <param name="writer">
        ///     The writer.
        /// </param>
        void WriteTo(KafkaBinaryWriter writer);
    }
}