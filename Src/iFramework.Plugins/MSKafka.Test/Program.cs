using IFramework.MessageQueue.MSKafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.MessageQueue.MSKafka.MessageFormat;
using IFramework.Config;
using Sample.Command;
using IFramework.Command;
using IFramework.IoC;
using Sample.DTO;
using IFramework.Infrastructure;
using System.Threading;
using System.Diagnostics;
using Kafka.Client.Consumers;

namespace MSKafka.Test
{
    class Program
    {
        static string commandQueue = "iframework.groupcommandqueue";
        static string zkConnectionString = "localhost:2181";

        static void Main(string[] args)
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
                Console.WriteLine($"consumer:{kafkaConsumer.ConsumerId} {DateTime.Now.ToString("HH:mm:ss.fff")} consume message: {message} cost: {(DateTime.Now - sendTime).TotalMilliseconds}");
                kafkaConsumer.CommitOffset(kafkaMessage.PartitionId.Value, kafkaMessage.Offset);
            };
            var consumer = new KafkaConsumer(zkConnectionString, commandQueue, $"{Environment.MachineName}.{commandQueue}", consumerId, onMessageReceived);
            return consumer;
        }

        static void GroupConsuemrTest()
        {
            var consumers = new List<KafkaConsumer>();
            for (int i = 0; i < 1; i++)
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
                message = $"{message} @{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}";
                var kafkaMessage = new Kafka.Client.Messages.Message(Encoding.UTF8.GetBytes(message));
                var data = new Kafka.Client.Producers.ProducerData<string, Kafka.Client.Messages.Message>(commandQueue, message, kafkaMessage);
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
                        Console.WriteLine("send message failed");
                        Thread.Sleep(2000);
                    }
                }

            }
        }

        static void ServiceTest()
        {
            ReduceProduct reduceProduct = new ReduceProduct
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
                ProductIds = new List<Guid> { reduceProduct.ProductId }
            }, true).Result.ReadAsAsync<List<Project>>().Result;

            Console.WriteLine(products.ToJson());
            Console.ReadLine();
        }
    }
}