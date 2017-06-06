using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.MessageQueue.ConfluentKafka;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;
using Kafka.Client.Consumers;
using Kafka.Client.Producers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSKafka = IFramework.MessageQueue.MSKafka;
using Kafka.Client.Messages;

namespace KafkaClient.Test
{
    [TestClass]
    public class KafkaClientTests
    {
        private static readonly string confluentCommandQueue = "confluent.groupcommandqueue";
        private static readonly string mscommandQueue = "ms.groupcommandqueue";
        private readonly string _brokerList = "localhost:9092";
        private readonly string _zkConnection = "localhost:2181";

        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance.UseUnityContainer()
                         .MessageQueueUseMachineNameFormat(false)
                         .UseLog4Net();
        }

        [TestMethod]
        public void CreateTopicTest()
        {
            try
            {
                var client = new MSKafka.KafkaClient(_zkConnection);
                client.CreateTopic(mscommandQueue);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetBaseException().Message);
                throw;
            }
        }

        [TestMethod]
        public void ConfluentProducerTest()
        {
            var queueClient = new KafkaProducer(confluentCommandQueue, _brokerList);

            var start = DateTime.Now;
            //var data = new ProducerData<string, Message>(commandQueue, message, kafkaMessage);
            var tasks = new List<Task>();
            for (int i = 0; i < 1000; i++)
            {
                var message = $"message:{i}";
                var kafkaMessage = new KafkaMessage(Encoding.UTF8.GetBytes(message));
                tasks.Add(queueClient.SendAsync(message, kafkaMessage));
            }
            Task.WhenAll(tasks).Wait();
            Console.WriteLine($"send message completed cost: {(DateTime.Now - start).TotalMilliseconds}");
            queueClient.Stop();
            //ZookeeperConsumerConnector.zkClientStatic?.Dispose();
        }


        [TestMethod]
        public void MSProducerTest()
        {
            var queueClient = new MSKafka.KafkaProducer(mscommandQueue, _zkConnection);

            var start = DateTime.Now;
           
            var tasks = new List<Task>();

            for (int i = 0; i < 1000; i++)
            {
                var message = $"message:{i}";
                var kafkaMessage = new Message(Encoding.UTF8.GetBytes(message));
                var data = new ProducerData<string, Message>(mscommandQueue, message, kafkaMessage);
                queueClient.Send(data);
            }
            
            Console.WriteLine($"send message completed cost: {(DateTime.Now - start).TotalMilliseconds}");
            queueClient.Stop();
            ZookeeperConsumerConnector.zkClientStatic?.Dispose();
        }


        [TestMethod]
        public void ConsumerTest()
        {
            var consumer = Program.CreateConsumer(confluentCommandQueue, "ConsumerTest");
            Thread.Sleep(100);
            consumer.Stop();
            //ZookeeperConsumerConnector.zkClientStatic?.Dispose();
        }
    }
}