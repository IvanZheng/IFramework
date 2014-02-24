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
        ICommandBus _CommandBus;
        public TestController(ICommandBus commandBus)
        {
            _CommandBus = commandBus;
        }
        
        // GET: /Test/
        public ActionResult Index()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
