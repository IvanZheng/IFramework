using System.Collections.Generic;

namespace Kafka.Client.Producers
{
    /// <summary>
    ///     Encapsulates data to be send on topic
    /// </summary>
    /// <typeparam name="TKey">
    ///     Type of partitioning key
    /// </typeparam>
    /// <typeparam name="TData">
    ///     Type of data
    /// </typeparam>
    public class ProducerData<TKey, TData>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerData{TKey,TData}" /> class.
        /// </summary>
        /// <param name="topic">
        ///     The topic.
        /// </param>
        /// <param name="key">
        ///     The partitioning key.
        /// </param>
        /// <param name="data">
        ///     The list of data to send on the same topic.
        /// </param>
        public ProducerData(string topic, TKey key, bool isKeyNull, IEnumerable<TData> data)
            : this(topic, key, data)
        {
            IsKeyNull = isKeyNull;
        }


        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerData{TKey,TData}" /> class.
        /// </summary>
        /// <param name="topic">
        ///     The topic.
        /// </param>
        /// <param name="key">
        ///     The partitioning key.
        /// </param>
        /// <param name="data">
        ///     The list of data to send on the same topic.
        /// </param>
        public ProducerData(string topic, TKey key, IEnumerable<TData> data)
            : this(topic, data)
        {
            Key = key;
            IsKeyNull = false;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerData{TKey,TData}" /> class.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="key">The partitioning key.</param>
        /// <param name="data">The data to send on the topic.</param>
        public ProducerData(string topic, TKey key, TData data)
            : this(topic, key, new List<TData> {data}) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerData{TKey,TData}" /> class.
        /// </summary>
        /// <param name="topic">
        ///     The topic.
        /// </param>
        /// <param name="data">
        ///     The list of data to send on the same topic.
        /// </param>
        public ProducerData(string topic, IEnumerable<TData> data)
        {
            Topic = topic;
            Data = data;
            IsKeyNull = true;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerData{TKey,TData}" /> class.
        /// </summary>
        /// <param name="topic">
        ///     The topic.
        /// </param>
        /// <param name="data">
        ///     The data to send on the topic.
        /// </param>
        public ProducerData(string topic, TData data)
            : this(topic, new List<TData> {data}) { }

        /// <summary>
        ///     Gets topic.
        /// </summary>
        public string Topic { get; }

        /// <summary>
        ///     Gets the partitioning key.
        /// </summary>
        public TKey Key { get; }

        /// <summary>
        ///     Whether partition key is Null or not
        /// </summary>
        public bool IsKeyNull { get; }

        /// <summary>
        ///     Gets the data.
        /// </summary>
        public IEnumerable<TData> Data { get; }
    }
}