using System;
using System.Text;
using System.Threading;
using IFramework.Config;
using IFramework.MessageQueue.MSKafka;
using Kafka.Client.Consumers;
using Kafka.Client.Messages;
using Kafka.Client.Producers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSKafka.Test
{
    [TestClass]
    public class KafkaClientTests
    {
        private static readonly string commandQueue = "seop.groupcommandqueue";
        private readonly string _zkConnection = "localhost:2181";

        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance.UseUnityContainer()
                         .MessageQueueUseMachineNameFormat(false)
                         .UseLog4Net("log4net.config");
        }

        [TestMethod]
        public void CreateTopicTest()
        {
            try
            {
                var client = new KafkaClient(_zkConnection);
                client.CreateTopic("testtopic3");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetBaseException().Message);
                throw;
            }
        }

        [TestMethod]
        public void ProducerTest()
        {
            var queueClient = new KafkaProducer(commandQueue, _zkConnection);

            var message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
            var kafkaMessage = new Message(Encoding.UTF8.GetBytes(message));
            var data = new ProducerData<string, Message>(commandQueue, message, kafkaMessage);

            queueClient.Send(data);
            Console.WriteLine($"send message: {message}");
            queueClient.Stop();
            ZookeeperConsumerConnector.zkClientStatic?.Dispose();
        }

        [TestMethod]
        public void ConsumerTest()
        {
            var consumer = Program.CreateConsumer(commandQueue, "ConsumerTest");
            Thread.Sleep(100);
            consumer.Stop();
            ZookeeperConsumerConnector.zkClientStatic?.Dispose();
        }
    }
}