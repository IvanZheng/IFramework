using IFramework.Command;
using IFramework.Infrastructure;
using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Sample.ApiService.Controllers
{
    public class TestController : Controller
    {
        // GET: /Test/
        public ActionResult Index()
        {
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
