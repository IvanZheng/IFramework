using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sample.CommandServiceCore.Authorizations;
using Sample.CommandServiceCore.Models;

namespace Sample.CommandServiceCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConcurrencyProcessor _concurrencyProcessor;
        private readonly IObjectProvider _objectProvider;
        private readonly ILogger _logger;
        public HomeController(IConcurrencyProcessor concurrencyProcessor,
                              ILogger<HomeController> logger,
                              IObjectProvider objectProvider)
        {
            _concurrencyProcessor = concurrencyProcessor;
            _objectProvider = objectProvider;
            _logger = logger;
        }

        [Authorize("AppAuthorization")]
        //[TypeFilter(typeof(AuthorizationFilterAttrubute))]
        //[AuthorizationFilterAttrubute]
        public Task<string> DoApi()
        {
            return _concurrencyProcessor.ProcessAsync(() => Task.Run(() => new {Name = "ivan"}.ToJson()));
        }

        public IActionResult Test()
        {
            return View();
        }


        public ApiResult PostAddRequest([FromBody]AddRequest request)
        {
            request = request ?? new AddRequest();
            request.File = Request.HasFormContentType ? Request.Form.Files.FirstOrDefault()?.FileName: null;
            return new ApiResult<AddRequest>(request);
        }

        public IActionResult Index()
        {
            using (_logger.BeginScope("begin scope"))
            {
                var profile = Configuration.Get("Debug");
                var member = Configuration.Get("Member:A");
                _logger.LogDebug(profile.ToJson());
                return View();
            }
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
