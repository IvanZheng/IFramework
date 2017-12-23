using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Config;
using Microsoft.AspNetCore.Mvc;
using Sample.CommandServiceCore.Models;

namespace Sample.CommandServiceCore.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Index()
        {
            var profile = Configuration.GetAppConfig("profile");
            Console.WriteLine(profile);
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
