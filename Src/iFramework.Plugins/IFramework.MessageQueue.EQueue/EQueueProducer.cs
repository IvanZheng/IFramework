using System;
using System.Collections.Generic;
using System.Net;
using EQueue.Clients.Producers;

namespace IFramework.MessageQueue.EQueue
{
    public class EQueueProducer
    {
        public EQueueProducer(string clusterName, List<IPEndPoint> nameServerList)
        {
            ClusterName = clusterName;
            NameServerList = nameServerList;
        }

        public Producer Producer { get; protected set; }
        public string ClusterName { get; protected set; }
        public List<IPEndPoint> NameServerList { get; protected set; }
        public int AdminPort { get; protected set; }

        public void Start()
        {
            var setting = new ProducerSetting
            {
                ClusterName = ClusterName,
                NameServerList = NameServerList
            };
            Producer = new Producer(setting).Start();
        }

        public void Stop()
        {
            Producer?.Shutdown();
        }

        public void Send(global::EQueue.Protocols.Message equeueMessage, string key)
        {
            if (key == null)
                key = string.Empty;
            var result = Producer.Send(equeueMessage, key);
            if (result.SendStatus != SendStatus.Success)
                throw new Exception(result.ErrorMessage);
        }
    }
}