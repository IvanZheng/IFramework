using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sample.CommandServiceCore.Models;

namespace Sample.CommandServiceCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IExceptionManager _exceptionManager;
        private readonly ILogger _logger;
        public HomeController(IExceptionManager exceptionManager,
                              ILoggerFactory loggerFactory)
        {
            _exceptionManager = exceptionManager;
            _logger = loggerFactory.CreateLogger(nameof(HomeController));
        }

        public Task<ApiResult<string>> DoApi()
        {
            return _exceptionManager.ProcessAsync(() => Task.Run(() => new {Name = "ivan"}.ToJson()));
        }

        public IActionResult Index()
        {
            var profile = Configuration.GetAppConfig("AppSettings:Debug");
            _logger.LogWarning(profile.ToJson());
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
