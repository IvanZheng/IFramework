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
    public class TestController : AsyncController
    {
        //
        // GET: /Test/
        public ActionResult Index()
        {
            return View();
        }


    }
}
