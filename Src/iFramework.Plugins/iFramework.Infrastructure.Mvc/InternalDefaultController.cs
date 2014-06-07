using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace IFramework.Infrastructure.Mvc
{
    class InternalDefaultController : Controller
    {
        public ActionResult Index()
        {
            return new EmptyResult();
        }
    }
}
