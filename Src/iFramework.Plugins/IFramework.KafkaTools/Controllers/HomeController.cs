using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using IFramework.KafkaTools.Models;

namespace IFramework.KafkaTools.Controllers
{
    public class HomeController : Controller
    {
        private readonly IObjectProvider _objectProvider;
        private readonly IServiceProvider _serviceProvider;

        public HomeController(IObjectProvider objectProvider, IServiceProvider serviceProvider)
        {
            _objectProvider = objectProvider;
            _serviceProvider = serviceProvider;
        }

        public IActionResult Index()
        {
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
