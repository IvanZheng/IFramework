using IFramework.Command;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using Sample.Command;
using Sample.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.CommandService.Tests
{
    public class CommandBusTests
    {
        ICommandBus _commandBus;

        List<CreateProduct> _createProducts;
        ILogger _logger;
        
        public CommandBusTests()
        {
            
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(CommandBusTests));

            _commandBus = IoCFactory.Resolve<ICommandBus>();

            _createProducts = new List<CreateProduct>();
            var tasks = new List<Task>();
            for (int i = 0; i< productCount; i ++)
            {
                var createProduct = new CreateProduct
                {
                    ProductId = Guid.NewGuid(),
                    Name = string.Format("{0}-{1}", DateTime.Now.ToString(), i),
                    Count = 20000
                };
                _createProducts.Add(createProduct);
                tasks.Add(_commandBus.SendAsync(createProduct, true).Result.Reply);
            }
            Task.WaitAll(tasks.ToArray());
        }

        int batchCount = 100;
        int productCount = 1;
        
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
                    tasks.Add(_commandBus.SendAsync(reduceProduct, true).Result.Reply);
                }
            }
            Task.WaitAll(tasks.ToArray());
            var costTime = (DateTime.Now - startTime).TotalMilliseconds;
            _logger.ErrorFormat("cost time : {0} ms", costTime);
        }
    }
}