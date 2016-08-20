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
        public IPAddress BrokerAddress { get; protected set; }
        public int ProducerPort { get; protected set; }
        public int AdminPort { get; protected set; }

        public EQueueProducer(string brokerAdress, int producerPort = 5000, int adminPort = 5002)
        {
            BrokerAddress = IPAddress.Parse(brokerAdress);
            ProducerPort = producerPort;
            AdminPort = adminPort;
        }

        public void Start()
        {
            var setting = new ProducerSetting
            {
                BrokerAddress = new IPEndPoint(BrokerAddress, ProducerPort),
                BrokerAdminAddress = new IPEndPoint(BrokerAddress, AdminPort)
            };
            Producer = new Producer(setting).Start();
        }

        internal void Send(global::EQueue.Protocols.Message equeueMessage, string key)
        {
            var result = Producer.Send(equeueMessage, key);
            if (result.SendStatus != SendStatus.Success)
            {
                throw new Exception(result.ErrorMessage);
            }
        }
    }
}
