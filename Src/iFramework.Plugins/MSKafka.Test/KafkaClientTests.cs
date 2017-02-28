using IFramework.Config;
using IFramework.MessageQueue.MSKafka;
using Kafka.Client.Consumers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSKafka.Test
{
    [TestClass()]
    public class KafkaClientTests
    {
        string _zkConnection = "localhost:2181";
        static string commandQueue = "seop.groupcommandqueue";

        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance.UseUnityContainer()
                                 .MessageQueueUseMachineNameFormat(false)
                                 .UseLog4Net("log4net.config");
        }

        [TestMethod()]
        public void CreateTopicTest()
        {
            try
            {
                KafkaClient client = new KafkaClient(_zkConnection);
                client.CreateTopic("testtopic1");
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
            var kafkaMessage = new Kafka.Client.Messages.Message(Encoding.UTF8.GetBytes(message));
            var data = new Kafka.Client.Producers.ProducerData<string, Kafka.Client.Messages.Message>(commandQueue, message, kafkaMessage);

            queueClient.Send(data);
            Console.WriteLine($"send message: {message}");
            queueClient.Stop();
            ZookeeperConsumerConnector.zkClientStatic?.Dispose();
        }

        [TestMethod]
        public void ConsumerTest()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var consumer = Program.CreateConsumer(commandQueue, "ConsumerTest");
            Thread.Sleep(100);
            consumer.Stop();
            ZookeeperConsumerConnector.zkClientStatic?.Dispose();
        }
    }
}
