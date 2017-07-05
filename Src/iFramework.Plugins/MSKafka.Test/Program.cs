using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using IFramework.Command;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.IoC;
using IFramework.MessageQueue.ConfluentKafka;
using IFramework.MessageQueue.ConfluentKafka.MessageFormat;
using Sample.Command;
using Sample.DTO;

//using IFramework.MessageQueue.MSKafka;

namespace KafkaClient.Test
{
    internal class Program
    {
        private static readonly string commandQueue = "confluentconsole.groupcommandqueue";
        private static readonly string zkConnectionString = "localhost:9092";

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
                var message = kafkaMessage.Value.Payload;
                var sendTime = DateTime.Parse(message.Split('@')[1]);
                Console.WriteLine(
                                  $"consumer:{kafkaConsumer.ConsumerId} {DateTime.Now.ToString("HH:mm:ss.fff")} consume message: {message} cost: {(DateTime.Now - sendTime).TotalMilliseconds} partition:{kafkaMessage.Partition} offset:{kafkaMessage.Offset}");
                kafkaConsumer.CommitOffsetAsync(kafkaMessage.Partition, kafkaMessage.Offset);
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
            var times = 0;
            while (true)
            {
                //if (times++ % 10 == 0)
                //{
                //    Task.Delay(10).Wait();
                //}
                //var message = string.Empty;
                var key = Console.ReadLine();
                if (key.Equals("q"))
                {
                    consumers.ForEach(consumer => consumer.Stop());
                    queueClient.Stop();
                    //ZookeeperConsumerConnector.zkClientStatic?.Dispose();
                    break;
                }
                //Console.WriteLine($"receive input {message}");
                var message = $"{key} @{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff}";
                var kafkaMessage = new KafkaMessage(message);
                //var data = new ProducerData<string, Message>(commandQueue, message, kafkaMessage);


                var start = DateTime.Now;
                queueClient.SendAsync(key, kafkaMessage, CancellationToken.None)
                           .ContinueWith(t =>
                           {
                               var result = t.Result;
                               Console.WriteLine($"send message: {message} partition:{result.Partition} offset:{result.Offset} cost: {(DateTime.Now - start).TotalMilliseconds}");
                           });
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