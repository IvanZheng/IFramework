using IFramework.AspNet;
using IFramework.Command;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Command;
using Sample.CommandServiceTests.Products;
using Sample.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Sample.CommandService.Tests
{
    [TestClass()]
    public class CommandBusTests
    {
        ICommandBus _commandBus;

        List<CreateProduct> _createProducts;
        ILogger _logger;

        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance.UseLog4Net()
                                  .MessageQueueUseMachineNameFormat(false);
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(CommandBusTests));

            _commandBus = IoCFactory.Resolve<ICommandBus>();
            _commandBus.Start();

            var handlerTest = new ProductCommandHandlerTest(batchCount, productCount);
            handlerTest.Initialize();
            _createProducts = handlerTest._createProducts;
            //_createProducts = new List<CreateProduct>();
            //var tasks = new List<Task>();
            //for (int i = 0; i< productCount; i ++)
            //{
            //    var createProduct = new CreateProduct
            //    {
            //        ProductId = Guid.NewGuid(),
            //        Name = string.Format("{0}-{1}", DateTime.Now.ToString(), i),
            //        Count = 20000
            //    };
            //    _createProducts.Add(createProduct);
            //    tasks.Add(_commandBus.Send(createProduct));
            //}
            //Task.WaitAll(tasks.ToArray());
        }

        int batchCount = 1000;
        int productCount = 2;

        [TestMethod()]
        public void CommandBusPressureTest()
        {
            var startTime = DateTime.Now;

            var tasks = new List<Task>();
            for (int i = 0; i < batchCount; i ++)
            {
                for (int j = 0; j < _createProducts.Count; j ++)
                {
                    ReduceProduct reduceProduct = new ReduceProduct
                    {
                        ProductId = _createProducts[j].ProductId,
                        ReduceCount = 1
                    };
                    tasks.Add(_commandBus.Send(reduceProduct));
                }
            }
            Task.WaitAll(tasks.ToArray());
            var costTime = (DateTime.Now - startTime).TotalMilliseconds;
            _logger.ErrorFormat("cost time : {0} ms", costTime);

            var products = _commandBus.Send<List<Project>>(new GetProducts
            {
                ProductIds = _createProducts.Select(p => p.ProductId).ToList()
            }).Result;

            for (int i = 0; i < _createProducts.Count; i++)
            {
                Assert.AreEqual(products.FirstOrDefault(p => p.Id == _createProducts[i].ProductId)
                                        .Count,
                                _createProducts[i].Count - batchCount);

            }
        }
    }
}