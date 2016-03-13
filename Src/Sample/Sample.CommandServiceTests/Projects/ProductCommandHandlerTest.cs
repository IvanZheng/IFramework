using IFramework.Config;
using IFramework.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Command;
using Sample.CommandHandler.Products;
using Sample.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.CommandServiceTests.Products
{
    [TestClass()]
    public class ProductCommandHandlerTest : CommandHandlerTest<ProdutCommandHandler>
    {
        public List<CreateProduct> _createProducts;
        int batchCount = 1000;
        int productCount = 2;


        public ProductCommandHandlerTest() { }
        public ProductCommandHandlerTest(int batchCount, int productCount) {
            this.batchCount = batchCount;
            this.productCount = productCount;
        }

        [TestInitialize]
        public void Initialize()
        {
            Configuration.Instance.UseLog4Net();
            _createProducts = new List<CreateProduct>();
            var tasks = new List<Task>();
            for (int i = 0; i < productCount; i++)
            {
                var createProduct = new CreateProduct
                {
                    ProductId = Guid.NewGuid(),
                    Name = string.Format("{0}-{1}", DateTime.Now.ToString(), i),
                    Count = 20000
                };
                _createProducts.Add(createProduct);
                ExecuteCommand(createProduct);
            }

        }

        [TestMethod]
        public void ReduceProduct()
        {
            var tasks = new List<Task>();
            for (int i = 0; i < batchCount; i++)
            {
                for (int j = 0; j < _createProducts.Count; j++)
                {
                    ReduceProduct reduceProduct = new ReduceProduct
                    {
                        ProductId = _createProducts[j].ProductId,
                        ReduceCount = 1
                    };
                    tasks.Add(new Task(() => ExceptionManager.Process(() => ExecuteCommand(reduceProduct), true)
                    ));
                }
            }
            tasks.AsParallel().ForEach(t => t.Start());
            Task.WaitAll(tasks.ToArray());

            var products = ExecuteCommand(new GetProducts
            {
                ProductIds = _createProducts.Select(p => p.ProductId).ToList()
            }) as List<DTO.Project>;

            for (int i = 0; i < _createProducts.Count; i++)
            {
                Assert.AreEqual(products.FirstOrDefault(p => p.Id == _createProducts[i].ProductId)
                                        .Count,
                                _createProducts[i].Count - batchCount);

            }

        }
    }
}
