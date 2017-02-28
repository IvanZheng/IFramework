using IFramework.Config;
using IFramework.MessageQueue;
using IFramework.MessageQueue.EQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EQueueTest
{
    class Program
    {
        static string topic = "groupcommandqueue";
        static string clusterName = "DefaultCluster";
        static List<IPEndPoint> NameServerList = ConfigurationEQueue.GetIPEndPoints("").ToList();
        static void Main(string[] args)
        {
            Configuration.Instance
                         .UseAutofacContainer()
                         .UseEQueue(clusterName: clusterName)
                         .UseNoneLogger();
            GroupConsuemrTest();
        }

        static EQueueConsumer CreateConsumer(string consumerId)
        {
            OnEQueueMessageReceived onMessageReceived = (equeueConsumer, queueMessage) =>
            {
                var message = Encoding.UTF8.GetString(queueMessage.Body);
                var sendTime = DateTime.Parse(message);
                Console.WriteLine($"consumer:{equeueConsumer.ConsumerId} {DateTime.Now.ToString("HH:mm:ss.fff")} consume message: {message} cost: {(DateTime.Now - sendTime).TotalMilliseconds}");
                equeueConsumer.CommitOffset(queueMessage.BrokerName, queueMessage.QueueId, queueMessage.QueueOffset);
            };

            var consumer = new EQueueConsumer(clusterName, NameServerList, topic, Environment.MachineName, consumerId, onMessageReceived);
            return consumer;
        }

        static void GroupConsuemrTest()
        {
            var consumers = new List<EQueueConsumer>();
            for (int i = 0; i < 3; i++)
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
                var queueMessage = new EQueue.Protocols.Message(topic, 1, Encoding.UTF8.GetBytes(message));

                while (true)
                {
                    try
                    {
                        producer.Send(queueMessage, message);
                        Console.WriteLine($"send message: {message}");
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
