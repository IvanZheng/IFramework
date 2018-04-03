using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.Command;

namespace Sample.CommandService.Tests
{
    public class CommandBusTests
    {
        private readonly ICommandBus _commandBus;

        private readonly List<CreateProduct> _createProducts;
        private readonly ILogger _logger;

        private readonly int batchCount = 100;
        private readonly int productCount = 1;

        public CommandBusTests()
        {
            _logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(typeof(CommandBusTests));

            _commandBus = ObjectProviderFactory.GetService<ICommandBus>();

            _createProducts = new List<CreateProduct>();
            var tasks = new List<Task>();
            for (var i = 0; i < productCount; i++)
            {
                var createProduct = new CreateProduct
                {
                    ProductId = Guid.NewGuid(),
                    Name = string.Format("{0}-{1}", DateTime.Now, i),
                    Count = 20000
                };
                _createProducts.Add(createProduct);
                tasks.Add(_commandBus.SendAsync(createProduct, true).Result.Reply);
            }
            Task.WaitAll(tasks.ToArray());
        }

        public void CommandBusPressureTest()
        {
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
                tasks.Add(_commandBus.SendAsync(reduceProduct, true).Result.Reply);
            }
            Task.WaitAll(tasks.ToArray());
            var costTime = (DateTime.Now - startTime).TotalMilliseconds;
            _logger.LogError("cost time : {0} ms", costTime);
        }
    }
}