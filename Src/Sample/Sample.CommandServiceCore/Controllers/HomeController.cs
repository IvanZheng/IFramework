﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.UnitOfWork;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sample.Applications;
using Sample.CommandServiceCore.Models;
using Sample.Domain;
using Sample.Domain.Model;
using Sample.Persistence;

namespace Sample.CommandServiceCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConcurrencyProcessor _concurrencyProcessor;
        private readonly SampleModelContext _dbContext;
        private readonly ICommunityService _communityService;
        private readonly ICommunityRepository _domainRepository;
        private readonly ILogger _logger;
        private readonly IObjectProvider _objectProvider;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(IConcurrencyProcessor concurrencyProcessor,
                              ILogger<HomeController> logger,
                              IObjectProvider objectProvider,
                              IUnitOfWork unitOfWork,
                              ICommunityRepository domainRepository,
                              SampleModelContext dbContext,
                              ICommunityService communityService)
        {
            _concurrencyProcessor = concurrencyProcessor;
            _objectProvider = objectProvider;
            _unitOfWork = unitOfWork;
            _domainRepository = domainRepository;
            _dbContext = dbContext;
            _communityService = communityService;
            _logger = logger;
        }

        //[Authorize("AppAuthorization")]
        //[TypeFilter(typeof(AuthorizationFilterAttrubute))]
        //[AuthorizationFilterAttrubute]
        [Transaction(Order = 1)]
        [LogInterceptor(Order = 2)]
        public virtual async Task<object> DoApi()
        {
            var sameProvider = _objectProvider.GetService<SampleModelContext>().GetHashCode() == HttpContext.RequestServices.GetService(typeof(SampleModelContext)).GetHashCode();
            await _communityService.ModifyUserEmailAsync(Guid.Empty, $"{DateTime.Now.Ticks}");
            return $"{DateTime.Now} DoApi Done! sameProvider:{sameProvider}";
        }

        public IActionResult Test()
        {
            return View();
        }


        public ApiResult PostAddRequest([FromBody] AddRequest request)
        {
            request = request ?? new AddRequest();
            request.File = Request.HasFormContentType ? Request.Form.Files.FirstOrDefault()?.FileName : null;
            return new ApiResult<AddRequest>(request);
        }

        public IActionResult Index([FromQuery] bool needGc)
        {
            using (_logger.BeginScope(new Dictionary<string, object> {{"needGc", needGc}}))
            {
                var profile = Configuration.Get("Debug");
                var member = Configuration.Get("Member:A");
                _logger.LogDebug(new {profile, member});
                _logger.LogDebug("index test");
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
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}