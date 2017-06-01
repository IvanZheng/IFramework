using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using IFramework.Command;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.IoC;
using IFramework.MessageQueue.MSKafka;
using Kafka.Client.Consumers;
using Kafka.Client.Messages;
using Kafka.Client.Producers;
using Sample.Command;
using Sample.DTO;

namespace MSKafka.Test
{
    internal class Program
    {
        private static readonly string commandQueue = "iframework.groupcommandqueue";
        private static readonly string zkConnectionString = "localhost:2181";

        private static void Main(string[] args)
        {
            Configuration.Instance
                         .UseUnityContainer()
                         .UseLog4Net("log4net.config");
            GroupConsuemrTest();
            IoCFactory.Instance.CurrentContainer.Dispose();
        }

        public static KafkaConsumer CreateConsumer(string commandQueue, string consumerId)
        {
            OnKafkaMessageReceived onMessageReceived = (kafkaConsumer, kafkaMessage) =>
            {
                var message = Encoding.UTF8.GetString(kafkaMessage.Payload);
                var sendTime = DateTime.Parse(message.Split('@')[1]);
                Console.WriteLine(
                                  $"consumer:{kafkaConsumer.ConsumerId} {DateTime.Now.ToString("HH:mm:ss.fff")} consume message: {message} cost: {(DateTime.Now - sendTime).TotalMilliseconds}");
                kafkaConsumer.CommitOffset(kafkaMessage.PartitionId.Value, kafkaMessage.Offset);
            };
            var consumer = new KafkaConsumer(zkConnectionString, commandQueue,
                                             $"{Environment.MachineName}.{commandQueue}", consumerId, onMessageReceived);
            return consumer;
        }

        private static void GroupConsuemrTest()
        {
            var consumers = new List<KafkaConsumer>();
            for (var i = 0; i < 1; i++)
            {
                consumers.Add(CreateConsumer(commandQueue, i.ToString()));
            }
            var queueClient = new KafkaProducer(commandQueue, zkConnectionString);
            while (true)
            {
                var message = Console.ReadLine();
                if (message.Equals("q"))
                {
                    consumers.ForEach(consumer => consumer.Stop());
                    queueClient.Stop();
                    ZookeeperConsumerConnector.zkClientStatic?.Dispose();
                    break;
                }
                message = $"{message} @{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff}";
                var kafkaMessage = new Message(Encoding.UTF8.GetBytes(message));
                var data = new ProducerData<string, Message>(commandQueue, message, kafkaMessage);
                while (true)
                {
                    try
                    {
                        queueClient.Send(data);
                        Console.WriteLine($"send message: {message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("send message failed {0}", ex.Message);
                        Thread.Sleep(2000);
                    }
                }
            }
        }

        private static void ServiceTest()
        {
            var reduceProduct = new ReduceProduct
            {
                ProductId = new Guid("2B6FDE83-A319-433B-9FA5-399D382D0CD3"),
                ReduceCount = 1
            };

            var _commandBus = IoCFactory.Resolve<ICommandBus>();
            _commandBus.Start();

            var t = _commandBus.SendAsync(reduceProduct, true).Result;
            Console.WriteLine(t.Reply.Result);


            var products = _commandBus.SendAsync(new GetProducts
                                      {
                                          ProductIds = new List<Guid> {reduceProduct.ProductId}
                                      }, true)
                                      .Result.ReadAsAsync<List<Project>>()
                                      .Result;

            Console.WriteLine(products.ToJson());
            Console.ReadLine();
        }
    }
}