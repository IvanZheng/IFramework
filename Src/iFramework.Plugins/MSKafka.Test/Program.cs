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

namespace MSKafka.Test
{
    class Program
    {
        static string commandQueue = "groupcommandqueue";
        static string zkConnectionString = "localhost:2181";

        static void Main(string[] args)
        {
            Configuration.Instance
                         .UseUnityContainer()
                         .UseLog4Net("log4net.config");
            GroupConsuemrTest();
        }

        public static Task CreateConsumerTask(string commandQueue, string consumerId, CancellationTokenSource cancellationTokenSource)
        {
            return Task.Run(() =>
            {
                var consumer = new KafkaConsumer(zkConnectionString, commandQueue, $"{Environment.MachineName}.{commandQueue}", consumerId);
                try
                {
                    foreach (var kafkaMessage in consumer.GetMessages(cancellationTokenSource.Token))
                    {
                        var message = Encoding.UTF8.GetString(kafkaMessage.Payload);
                        var sendTime = DateTime.Parse(message);
                        Console.WriteLine($"consumer:{consumer.ConsumerId} {DateTime.Now.ToString("HH:mm:ss.fff")} consume message: {message} cost: {(DateTime.Now - sendTime).TotalMilliseconds}");
                        consumer.CommitOffset(kafkaMessage.PartitionId.Value, kafkaMessage.Offset);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        Console.WriteLine(ex.GetBaseException().Message);
                    }
                }
                finally
                {
                    consumer.Stop();
                }
            });
        }

        static void GroupConsuemrTest()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var consumerTasks = new List<Task>();
            for (int i = 0; i < 1; i++)
            {
                consumerTasks.Add(CreateConsumerTask(commandQueue, i.ToString(), cancellationTokenSource));
            }


            var queueClient = new KafkaProducer(commandQueue, zkConnectionString);
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
                var kafkaMessage = new Kafka.Client.Messages.Message(Encoding.UTF8.GetBytes(message));
                var data = new Kafka.Client.Producers.ProducerData<string, Kafka.Client.Messages.Message>(commandQueue, message, kafkaMessage);
                try
                {
                    queueClient.Send(data);
                    Console.WriteLine($"send message: {message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetBaseException().Message);
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
