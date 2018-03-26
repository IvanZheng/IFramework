using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using IFramework.Infrastructure;
using Microsoft.Extensions.Logging;
using Sample.CommandService.Tests;

namespace Sample.ApiService.Controllers
{
    //[Authorize]
    public class TestController : Controller
    {
        private readonly IExceptionManager _exceptionManager;
        private readonly ILogger _logger;

        public TestController(IExceptionManager exceptionManager, ILogger<TestController> logger)
        {
            _exceptionManager = exceptionManager;
            _logger = logger;
        }
        // GET: /Test/
        public ActionResult Index()
        {
            _logger.LogDebug("TestController Index");
            return View();
        }

        public ActionResult Mailbox()
        {
            var test = new CommandBusTests();
            test.CommandBusPressureTest();
            return View();
        }

        [HttpGet]
        public Task<string> CommandDistributorStatus()
        {
            return Task.Factory.StartNew(() =>
            {
                //var commandDistributor = IoCFactory.Resolve<IMessageConsumer>("CommandDistributor");
                //var domainEventConsumer = IoCFactory.Resolve<IMessageConsumer>("DomainEventConsumer");
                //var distributorStatus = commandDistributor.GetStatus() +
                //    "event consumer:" + domainEventConsumer.GetStatus();
                //return distributorStatus;
                return "";
            });
        }
    }
}