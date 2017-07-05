using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using EQueue.Protocols;
using IFramework.Config;
using IFramework.MessageQueue.EQueue;

namespace EQueueTest
{
    internal class Program
    {
        private static readonly string topic = "groupcommandqueue";
        private static readonly string clusterName = "DefaultCluster";
        private static readonly List<IPEndPoint> NameServerList = ConfigurationEQueue.GetIPEndPoints("").ToList();

        private static void Main(string[] args)
        {
            Configuration.Instance
                         .UseAutofacContainer()
                         .UseEQueue(clusterName: clusterName)
                         .UseNoneLogger();
            GroupConsuemrTest();
        }

        private static EQueueConsumer CreateConsumer(string consumerId)
        {
            OnEQueueMessageReceived onMessageReceived = (equeueConsumer, queueMessage) =>
            {
                var message = Encoding.UTF8.GetString(queueMessage.Body);
                var sendTime = DateTime.Parse(message);
                Console.WriteLine(
                                  $"consumer:{equeueConsumer.ConsumerId} {DateTime.Now.ToString("HH:mm:ss.fff")} consume message: {message} cost: {(DateTime.Now - sendTime).TotalMilliseconds}");
                equeueConsumer.CommitOffset(queueMessage.BrokerName, queueMessage.QueueId, queueMessage.QueueOffset);
            };

            var consumer = new EQueueConsumer(clusterName, NameServerList, topic, Environment.MachineName, consumerId,
                                              onMessageReceived);
            return consumer;
        }

        private static void GroupConsuemrTest()
        {
            var consumers = new List<EQueueConsumer>();
            for (var i = 0; i < 3; i++)
            {
                consumers.Add(CreateConsumer(i.ToString()));
            }
            var producer = new EQueueProducer(clusterName, NameServerList);
            producer.Start();
            while (true)
            {
                var message = Console.ReadLine();
                if (message.Equals("q"))
                {
                    consumers.ForEach(consumer => consumer.Stop());
                    break;
                }
                message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                var queueMessage = new Message(topic, 1, Encoding.UTF8.GetBytes(message));

                while (true)
                {
                    try
                    {
                        producer.SendAsync(queueMessage, message, CancellationToken.None)
                                .ContinueWith(t =>
                                {
                                    Console.WriteLine($"send message: {message}");
                                });
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.GetBaseException().Message);
                        Thread.Sleep(2000);
                    }
                }
            }
        }
    }
}