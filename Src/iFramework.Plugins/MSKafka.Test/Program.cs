﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
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
        private static readonly string brokerList = "localhost:9092";

        private static void Main(string[] args)
        {
            Configuration.Instance
                         .UseUnityContainer()
                         .UseJsonNet()
                         .UseLog4Net("log4net.config");
            GroupConsuemrTest();
            IoCFactory.Instance.CurrentContainer.Dispose();
        }

        public static KafkaConsumer<string, KafkaMessage> CreateConsumer(string commandQueue, string consumerId)
        {
            void OnMessageReceived(KafkaConsumer<string, KafkaMessage> kafkaConsumer, Message<string, KafkaMessage> kafkaMessage)
            {
                var message = kafkaMessage.Value.Payload;
                var sendTime = DateTime.Parse(message.Split('@')[1]);
                Console.WriteLine($"consumer:{kafkaConsumer.ConsumerId} {DateTime.Now:HH:mm:ss.fff} consume message: {message} cost: {(DateTime.Now - sendTime).TotalMilliseconds} partition:{kafkaMessage.Partition} offset:{kafkaMessage.Offset}");
                kafkaConsumer.CommitOffset(kafkaMessage.Partition, kafkaMessage.Offset);
            }

            var consumer = new KafkaConsumer<string, KafkaMessage>(brokerList, commandQueue,
                                                                   $"{Environment.MachineName}.{commandQueue}",
                                                                   consumerId,
                                                                   OnMessageReceived,
                                                                   new StringDeserializer(Encoding.UTF8),
                                                                   new KafkaMessageDeserializer());
            return consumer;
        }

        private static void GroupConsuemrTest()
        {
            var consumers = new List<KafkaConsumer<string, KafkaMessage>>();
            for (var i = 0; i < 1; i++)
            {
                consumers.Add(CreateConsumer(commandQueue, i.ToString()));
            }
            var queueClient = new KafkaProducer<string, KafkaMessage>(commandQueue, brokerList, new StringSerializer(Encoding.UTF8), new KafkaMessageSerializer());
            while (true)
            {
                var key = Console.ReadLine();
                if (key.Equals("q"))
                {
                    consumers.ForEach(consumer => consumer.Stop());
                    queueClient.Stop();
                    break;
                }

                var message = $"{key} @{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff}";
                var kafkaMessage = new KafkaMessage(message);

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