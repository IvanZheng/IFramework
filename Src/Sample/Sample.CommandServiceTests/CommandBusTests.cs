using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Config;
using IFramework.EntityFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.MessageQueue;
using IFramework.MessageQueue.MSKafka;
using IFramework.MessageQueue.MSKafka.Config;
using Kafka.Client.Consumers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Command;
using Sample.CommandServiceTests.Products;
using Sample.DTO;

namespace Sample.CommandService.Tests
{
    [TestClass]
    public class CommandBusTests
    {
        private static readonly string _zkConnectionString = "localhost:2181";

        private readonly int batchCount = 10;
        private readonly int productCount = 10;
        private ICommandBus _commandBus;
        private List<CreateProduct> _createProducts;
        private ILogger _logger;

        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance.UseUnityContainer()
                         .UseLog4Net()
                         .UseKafka(_zkConnectionString)
                         //    .SetCommitPerMessage(true)//for servicebus !!!
                         .MessageQueueUseMachineNameFormat()
                         .UseCommandBus("CommandBusTest", "CommandBusTest.ReplyTopic", "CommandBusTest.ReplySubscription")
                         .RegisterDefaultEventBus()
                         .RegisterEntityFrameworkComponents();

            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(CommandBusTests));
            InitProducts();
        }


        public void InitProducts()
        {
            var handlerTest = new ProductCommandHandlerTest(batchCount, productCount);
            handlerTest.Initialize();
            _createProducts = handlerTest._createProducts;
        }

        [TestMethod]
        public void CommandBusReduceProductTest()
        {
            _commandBus = MessageQueueFactory.GetCommandBus();
            _commandBus.Start();
            var startTime = DateTime.Now;
            var reduceProduct = new ReduceProduct
            {
                ProductId = _createProducts[0].ProductId,
                ReduceCount = 1
            };
            var t = _commandBus.SendAsync(reduceProduct, true).Result;
            Console.WriteLine(t.Reply.Result);

            var costTime = (DateTime.Now - startTime).TotalMilliseconds;
            _logger.ErrorFormat("cost time : {0} ms", costTime);

            var products = _commandBus.SendAsync(new GetProducts
                                      {
                                          ProductIds = _createProducts.Select(p => p.ProductId).ToList()
                                      }, true)
                                      .Result.ReadAsAsync<List<Project>>()
                                      .Result;
            var success = true;
            Console.WriteLine(products.ToJson());
            for (var i = 0; i < _createProducts.Count; i++)
            {
                success = success && products.FirstOrDefault(p => p.Id == _createProducts[i].ProductId)
                                             .Count ==
                          _createProducts[i].Count - batchCount;
            }
            Console.WriteLine($"test success {success}");
            Stop();
        }


        [TestMethod]
        public void CommandBusExecuteAsyncTest()
        {
            _commandBus = MessageQueueFactory.GetCommandBus();
            _commandBus.Start();
            var reduceProduct = new ReduceProduct
            {
                ProductId = _createProducts.First().ProductId,
                ReduceCount = 1
            };
            var result = _commandBus.ExecuteAsync(reduceProduct).Result;
            Stop();
        }

        public static KafkaConsumer CreateConsumer(string commandQueue, string consumerId)
        {
            OnKafkaMessageReceived onMessageReceived = (kafkaConsumer, kafkaMessage) =>
            {
                var message = Encoding.UTF8.GetString(kafkaMessage.Payload);
                var sendTime = DateTime.Parse(message);
                Console.WriteLine(
                                  $"consumer:{kafkaConsumer.ConsumerId} {DateTime.Now.ToString("HH:mm:ss.fff")} consume message: {message} cost: {(DateTime.Now - sendTime).TotalMilliseconds}");
                kafkaConsumer.CommitOffset(kafkaMessage.PartitionId.Value, kafkaMessage.Offset);
            };

            var consumer = new KafkaConsumer(_zkConnectionString, commandQueue,
                                             $"{Environment.MachineName}.{commandQueue}", consumerId, onMessageReceived);
            return consumer;
        }

        [TestMethod]
        public void ConsumerTest()
        {
            var commandQueue = "seop.groupcommandqueue";
            var cancellationTokenSource = new CancellationTokenSource();
            var consumer = CreateConsumer(commandQueue, "ConsumerTest");
            Thread.Sleep(100);
            consumer.Stop();
            ZookeeperConsumerConnector.zkClientStatic?.Dispose();
        }

        [TestMethod]
        public void CommandBusPressureTest()
        {
            _commandBus = MessageQueueFactory.GetCommandBus();
            _commandBus.Start();
            var startTime = DateTime.Now;
            var tasks = new List<Task>();
            for (var i = 0; i < batchCount; i++)
            for (var j = 0; j < _createProducts.Count; j++)
            {
                var reduceProduct = new ReduceProduct
                {
                    ProductId = _createProducts[j].ProductId,
                    ReduceCount = 1
                };
                var t = _commandBus.ExecuteAsync(reduceProduct);
                tasks.Add(t);
            }
            Task.WaitAll(tasks.ToArray());
            var costTime = (DateTime.Now - startTime).TotalMilliseconds;
            Console.WriteLine("cost time : {0} ms", costTime);

            var products = _commandBus.ExecuteAsync<List<Project>>(new GetProducts
                                      {
                                          ProductIds = _createProducts.Select(p => p.ProductId).ToList()
                                      })
                                      .Result;
            var success = true;

            for (var i = 0; i < _createProducts.Count; i++)
            {
                success = success && products.FirstOrDefault(p => p.Id == _createProducts[i].ProductId)
                                             .Count ==
                          _createProducts[i].Count - batchCount;
            }
            Console.WriteLine($"test success {success}");
            Assert.IsTrue(success);
            Stop();
        }


        public void Stop()
        {
            _commandBus.Stop();
            IoCFactory.Instance.CurrentContainer.Dispose();
        }
    }
}