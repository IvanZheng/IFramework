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
        private readonly IExceptionManager _exceptionManager;
        private readonly IObjectProvider _objectProvider;
        private readonly ILogger _logger;
        public HomeController(IExceptionManager exceptionManager,
                              ILoggerFactory loggerFactory,
                              IObjectProvider objectProvider)
        {
            _exceptionManager = exceptionManager;
            _objectProvider = objectProvider;
            _logger = loggerFactory.CreateLogger(nameof(HomeController));
        }

        [Authorize("AppAuthorization")]
        //[TypeFilter(typeof(AuthorizationFilterAttrubute))]
        //[AuthorizationFilterAttrubute]
        public Task<ApiResult<string>> DoApi()
        {
            return _exceptionManager.ProcessAsync(() => Task.Run(() => new {Name = "ivan"}.ToJson()));
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
            var profile = Configuration.Get("Debug");
            var member = Configuration.Get("Member:A");
            _logger.LogDebug(profile.ToJson());
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
