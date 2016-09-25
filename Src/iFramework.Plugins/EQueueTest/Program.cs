using IFramework.Config;
using IFramework.MessageQueue.EQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EQueueTest
{
    class Program
    {
        static string topic = "groupcommandqueue";
        static string brokerAddress = "192.168.199.242";
        static int consumerPort = 5001;
        static int adminPort = 5002;
        static void Main(string[] args)
        {
            Configuration.Instance
                         .UseAutofacContainer()
                         .UseEQueue(brokerAddress)
                         .UseNoneLogger();
            GroupConsuemrTest();
        }

        static Task CreateConsumerTask(string consumerId, CancellationTokenSource cancellationTokenSource)
        {
            return Task.Run(() =>
            {
                var consumer = new EQueueConsumer(brokerAddress, consumerPort, adminPort, topic, Environment.MachineName, consumerId);
                consumer.Start();
                while (true)
                {
                    try
                    {
                        foreach (var queueMessage in consumer.PullMessages(100, 2000, cancellationTokenSource.Token))
                        {
                            var message = Encoding.UTF8.GetString(queueMessage.Body);
                            var sendTime = DateTime.Parse(message);
                            Console.WriteLine($"consumer:{consumer.ConsumerId} {DateTime.Now.ToString("HH:mm:ss.fff")} consume message: {message} cost: {(DateTime.Now - sendTime).TotalMilliseconds}");
                            consumer.CommitOffset(queueMessage.BrokerName, queueMessage.QueueId, queueMessage.QueueOffset);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ThreadAbortException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationTokenSource.IsCancellationRequested)
                        {
                            Console.WriteLine(ex.GetBaseException().Message);
                        }
                    }
                }
                consumer.Stop();
            });
        }

        static void GroupConsuemrTest()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var consumerTasks = new List<Task>();
            for (int i = 0; i < 3; i++)
            {
                consumerTasks.Add(CreateConsumerTask(i.ToString(), cancellationTokenSource));
            }


            var producer = new EQueueProducer(brokerAddress);
            producer.Start();
            while (true)
            {
                var message = Console.ReadLine();
                if (message.Equals("q"))
                {
                    cancellationTokenSource.Cancel();
                    Task.WaitAll(consumerTasks.ToArray());
                    break;
                }
                message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                var queueMessage = new EQueue.Protocols.Message(topic, 1, Encoding.UTF8.GetBytes(message));
                producer.Send(queueMessage, message);
                Console.WriteLine($"send message: {message}");
            }
        }
    }
}
