using EQueue.Clients.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EQueue.Protocols;

namespace IFramework.MessageQueue.EQueue
{
    public class EQueueProducer
    {
        public Producer Producer { get; protected set; }
        public string ClusterName { get; protected set; }
        public List<IPEndPoint> NameServerList { get; protected set; }
        public int AdminPort { get; protected set; }

        public EQueueProducer(string clusterName, List<IPEndPoint> nameServerList)
        {
            ClusterName = clusterName;
            NameServerList = nameServerList;
        }

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
            {
                key = string.Empty;
            }
            var result = Producer.Send(equeueMessage, key);
            if (result.SendStatus != SendStatus.Success)
            {
                throw new Exception(result.ErrorMessage);
            }
        }
    }
}
