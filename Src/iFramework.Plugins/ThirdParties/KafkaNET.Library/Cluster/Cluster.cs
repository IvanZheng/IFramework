using System;
using System.Collections.Generic;
using System.Globalization;
using Kafka.Client.ZooKeeperIntegration;

namespace Kafka.Client.Cluster
{
    /// <summary>
    ///     The set of active brokers in the cluster
    /// </summary>
    internal class Cluster
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Cluster" /> class.
        /// </summary>
        public Cluster()
        {
            Brokers = new Dictionary<int, Broker>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Cluster" /> class.
        /// </summary>
        /// <param name="zkClient">IZooKeeperClient object</param>
        public Cluster(IZooKeeperClient zkClient) : this()
        {
            var nodes = zkClient.GetChildrenParentMayNotExist(ZooKeeperClient.DefaultBrokerIdsPath);
            foreach (var node in nodes)
            {
                var brokerZkString = zkClient.ReadData<string>(ZooKeeperClient.DefaultBrokerIdsPath + "/" + node);
                var broker = CreateBroker(node, brokerZkString);
                Brokers[broker.Id] = broker;
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Cluster" /> class.
        /// </summary>
        /// <param name="brokers">
        ///     The set of active brokers.
        /// </param>
        public Cluster(IEnumerable<Broker> brokers) : this()
        {
            foreach (var broker in brokers)
                Brokers.Add(broker.Id, broker);
        }

        /// <summary>
        ///     Gets brokers collection
        /// </summary>
        public Dictionary<int, Broker> Brokers { get; }

        /// <summary>
        ///     Gets broker with given ID
        /// </summary>
        /// <param name="id">
        ///     The broker ID.
        /// </param>
        /// <returns>
        ///     The broker with given ID
        /// </returns>
        public Broker GetBroker(int id)
        {
            if (Brokers.ContainsKey(id))
                return Brokers[id];

            return null;
        }

        /// <summary>
        ///     Creates a new Broker object out of a BrokerInfoString
        /// </summary>
        /// <param name="node">node string</param>
        /// <param name="brokerInfoString">the BrokerInfoString</param>
        /// <returns>Broker object</returns>
        private Broker CreateBroker(string node, string brokerInfoString)
        {
            int id;
            if (int.TryParse(node, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                return Broker.CreateBroker(id, brokerInfoString);

            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "{0} is not a valid integer", node));
        }
    }
}