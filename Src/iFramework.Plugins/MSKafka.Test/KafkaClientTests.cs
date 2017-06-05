using System;
using System.Text;
using System.Threading;
using IFramework.Config;
using IFramework.MessageQueue.ConfluentKafka;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSKafka.Test
{
    [TestClass]
    public class KafkaClientTests
    {
        private static readonly string commandQueue = "seop.groupcommandqueue";
        private readonly string _zkConnection = "localhost:9092";

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
                var client = new ConfluentKafkaClient(_zkConnection);
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
            var kafkaMessage = new KafkaMessage(Encoding.UTF8.GetBytes(message));
            //var data = new ProducerData<string, Message>(commandQueue, message, kafkaMessage);

            queueClient.Send(message, kafkaMessage);
            Console.WriteLine($"send message: {message}");
            queueClient.Stop();
            //ZookeeperConsumerConnector.zkClientStatic?.Dispose();
        }

        [TestMethod]
        public void ConsumerTest()
        {
            var consumer = Program.CreateConsumer(commandQueue, "ConsumerTest");
            Thread.Sleep(100);
            consumer.Stop();
            //ZookeeperConsumerConnector.zkClientStatic?.Dispose();
        }
    }
}