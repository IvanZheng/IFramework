using IFramework.Command;
using IFramework.Config;
using IFramework.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Command;
using Sample.CommandService.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.CommandService.Controllers.Tests
{
    [TestClass()]
    public class CommandBusTests
    {
        ICommandBus _commandBus;
        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance.UseLog4Net()
                                  .MessageQueueUseMachineNameFormat(false);
            _commandBus = IoCFactory.Resolve<ICommandBus>();
            _commandBus.Start();
        }

        [TestMethod()]
        public void CommandBusPressureTest()
        {
            var batchCount = 200;

            CreateProduct createProduct = new CreateProduct
            {
                ProductId = Guid.NewGuid(),
                Name = DateTime.Now.ToString(),
                Count = 1000
            };
            CreateProduct createProduct2 = new CreateProduct
            {
                ProductId = Guid.NewGuid(),
                Name = DateTime.Now.ToString(),
                Count = 2000
            };

            _commandBus.Send(createProduct).Wait();
            _commandBus.Send(createProduct2).Wait();


            var tasks = new List<Task>();
            for (int i = 0; i < batchCount; i ++)
            {
                ReduceProduct reduceProduct = new ReduceProduct
                {
                    ProductId = createProduct.ProductId,
                    ReduceCount = 1
                };
                ReduceProduct reduceProduct2 = new ReduceProduct
                {
                    ProductId = createProduct2.ProductId,
                    ReduceCount = 1
                };
                tasks.Add(_commandBus.Send(reduceProduct));
                tasks.Add(_commandBus.Send(reduceProduct2));

            }
            Task.WaitAll(tasks.ToArray());

            var currentCount = _commandBus.Send<int>(new GetProduct { ProductId = createProduct.ProductId }).Result;
            var currentCount2 = _commandBus.Send<int>(new GetProduct { ProductId = createProduct2.ProductId }).Result;

            Assert.AreEqual(currentCount, createProduct.Count - batchCount);
            Assert.AreEqual(currentCount2, createProduct2.Count - batchCount);

        }
    }
}