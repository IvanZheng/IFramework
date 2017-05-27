using System.Text;
using Kafka.Client.Utils;

namespace Kafka.Client.ZooKeeperIntegration
{
    /// <summary>
    ///     Zookeeper is able to store data in form of byte arrays. This interfacte is a bridge between those byte-array format
    ///     and higher level objects.
    /// </summary>
    public class ZooKeeperStringSerializer : IZooKeeperSerializer
    {
        public static readonly ZooKeeperStringSerializer Serializer = new ZooKeeperStringSerializer();

        /// <summary>
        ///     Prevents a default instance of the <see cref="ZooKeeperStringSerializer" /> class from being created.
        /// </summary>
        private ZooKeeperStringSerializer()
        {
        }

        /// <summary>
        ///     Serializes data using UTF-8 encoding
        /// </summary>
        /// <param name="obj">
        ///     The data to serialize
        /// </param>
        /// <returns>
        ///     Serialized data
        /// </returns>
        public byte[] Serialize(object obj)
        {
            Guard.NotNull(obj, "obj");
            return Encoding.UTF8.GetBytes(obj.ToString());
        }

        /// <summary>
        ///     Deserializes data using UTF-8 encoding
        /// </summary>
        /// <param name="bytes">
        ///     The serialized data
        /// </param>
        /// <returns>
        ///     The deserialized data
        /// </returns>
        public object Deserialize(byte[] bytes)
        {
            return bytes == null ? null : Encoding.UTF8.GetString(bytes);
        }
    }
}