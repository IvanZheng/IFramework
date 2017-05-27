namespace Kafka.Client.ZooKeeperIntegration
{
    /// <summary>
    ///     Zookeeper is able to store data in form of byte arrays. This interfacte is a bridge between those byte-array format
    ///     and higher level objects.
    /// </summary>
    public interface IZooKeeperSerializer
    {
        /// <summary>
        ///     Serializes data
        /// </summary>
        /// <param name="obj">
        ///     The data to serialize
        /// </param>
        /// <returns>
        ///     Serialized data
        /// </returns>
        byte[] Serialize(object obj);

        /// <summary>
        ///     Deserializes data
        /// </summary>
        /// <param name="bytes">
        ///     The serialized data
        /// </param>
        /// <returns>
        ///     The deserialized data
        /// </returns>
        object Deserialize(byte[] bytes);
    }
}