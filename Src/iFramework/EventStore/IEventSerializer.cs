namespace IFramework.EventStore
{
    public interface IEventSerializer
    {
        /// <summary>
        ///     Serialize the key or value of a <see cref="T:Confluent.Kafka.Message`2" />
        ///     instance.
        /// </summary>
        /// <param name="data">The value to serialize.</param>
        /// <returns>The serialized value.</returns>
        byte[] Serialize(object data);
    }
}
