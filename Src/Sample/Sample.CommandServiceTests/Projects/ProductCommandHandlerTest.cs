using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.Message;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Command;
using Sample.CommandHandler.Products;
using Sample.DTO;

namespace Sample.CommandServiceTests.Products
{
    [TestClass]
    public class ProductCommandHandlerTest : CommandHandlerProxy<ProdutCommandHandler>
    {
        public List<CreateProduct> _createProducts;
        private readonly int batchCount = 50000;
        private readonly int productCount = 2;


        public ProductCommandHandlerTest()
        {
        }

        public ProductCommandHandlerTest(int batchCount, int productCount)
        {
            this.batchCount = batchCount;
            this.productCount = productCount;
        }

        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance.UseLog4Net();
            _createProducts = new List<CreateProduct>();
            var tasks = new List<Task>();
            for (var i = 0; i < productCount; i++)
            {
                var createProduct = new CreateProduct
                {
                    ProductId = Guid.NewGuid(),
                    Name = string.Format("{0}-{1}", DateTime.Now, i),
                    Count = 200000
                };
                _createProducts.Add(createProduct);
                ExecuteCommand(createProduct);
            }
        }

        [TestMethod]
        public void ReduceProduct()
        {
            var tasks = new List<Task>();
            for (var i = 0; i < batchCount; i++)
            for (var j = 0; j < _createProducts.Count; j++)
            {
                var reduceProduct = new ReduceProduct
                {
                    ProductId = _createProducts[j].ProductId,
                    ReduceCount = 1
                };
                tasks.Add(Task.Run(() => ExceptionManager.Process(() => ExecuteCommand(reduceProduct), true)));
            }
            Task.WaitAll(tasks.ToArray());

            var products = ExecuteCommand(new GetProducts
            {
                ProductIds = _createProducts.Select(p => p.ProductId).ToList()
            }) as List<Project>;

            for (var i = 0; i < _createProducts.Count; i++)
                Assert.AreEqual(products.FirstOrDefault(p => p.Id == _createProducts[i].ProductId)
                        .Count,
                    _createProducts[i].Count - batchCount);
        }

        //[TestMethod]
        //public void ReduceProductByProcessorScheduler()
        //{
        //    var tasks = new List<Task>();

        //    var processorScheduler = new DefaultProcessingMessageScheduler<ICommand>();

        //    for (int i = 0; i < batchCount; i++)
        //    {
        //        for (int j = 0; j < _createProducts.Count; j++)
        //        {
        //            ReduceProduct reduceProduct = new ReduceProduct
        //            {
        //                ProductId = _createProducts[j].ProductId,
        //                ReduceCount = 1
        //            };
        //            tasks.Add(processorScheduler.SchedulProcessing(() => ExceptionManager.Process(() => ExecuteCommand(reduceProduct), true)));
        //        }
        //    }

        //    Task.WaitAll(tasks.ToArray());
        //    var products = ExecuteCommand(new GetProducts
        //    {
        //        ProductIds = _createProducts.Select(p => p.ProductId).ToList()
        //    }) as List<DTO.Project>;

        //    for (int i = 0; i < _createProducts.Count; i++)
        //    {
        //        Assert.AreEqual(products.FirstOrDefault(p => p.Id == _createProducts[i].ProductId)
        //                                .Count,
        //                        _createProducts[i].Count - batchCount);

        //    }
        //}


        /// <summary>
        ///     the batch count is more, the performance improves more compared with by mailbox and by optimisticConcurrency
        ///     control
        /// </summary>
        [TestMethod]
        public void ReduceProductByMailbox()
        {
            var messageProcessor = new MessageProcessor(new DefaultProcessingMessageScheduler<IMessageContext>());
            messageProcessor.Start();
            for (var i = 0; i < batchCount; i++)
            for (var j = 0; j < _createProducts.Count; j++)
            {
                var reduceProduct = new ReduceProduct
                {
                    ProductId = _createProducts[j].ProductId,
                    ReduceCount = 1
                };
                var commandContext =
                    new MessageContext {Message = reduceProduct, Key = reduceProduct.ProductId.ToString()};

                messageProcessor.Process(commandContext, messageContext => ExecuteCommand(messageContext));
            }
            do
            {
                Task.Delay(100).Wait();
            } while (ProcessingMailbox<IMessageContext>.ProcessedCount != batchCount * productCount);

            var products = ExecuteCommand(new GetProducts
            {
                ProductIds = _createProducts.Select(p => p.ProductId).ToList()
            }) as List<Project>;

            for (var i = 0; i < _createProducts.Count; i++)
                Assert.AreEqual(products.FirstOrDefault(p => p.Id == _createProducts[i].ProductId)
                        .Count,
                    _createProducts[i].Count - batchCount);
        }
    }
}