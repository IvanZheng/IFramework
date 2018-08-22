using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sample.CommandServiceCore.Authorizations;
using Sample.CommandServiceCore.Models;
using Sample.Domain;
using Sample.Domain.Model;
using Sample.Persistence;

namespace Sample.CommandServiceCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConcurrencyProcessor _concurrencyProcessor;
        private readonly IObjectProvider _objectProvider;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommunityRepository _domainRepository;
        private readonly SampleModelContext _dbContext;
        private readonly ILogger _logger;
        public HomeController(IConcurrencyProcessor concurrencyProcessor,
                              ILogger<HomeController> logger,
                              IObjectProvider objectProvider,
                              IUnitOfWork unitOfWork,
                              ICommunityRepository domainRepository,
                              SampleModelContext dbContext)
        {
            _concurrencyProcessor = concurrencyProcessor;
            _objectProvider = objectProvider;
            _unitOfWork = unitOfWork;
            _domainRepository = domainRepository;
            _dbContext = dbContext;
            _logger = logger;
        }

        //[Authorize("AppAuthorization")]
        //[TypeFilter(typeof(AuthorizationFilterAttrubute))]
        //[AuthorizationFilterAttrubute]
        public async Task DoApi()
        {
            //using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
            //                                                                new TransactionOptions
            //                                                                {
            //                                                                    IsolationLevel = IsolationLevel.ReadCommitted
            //                                                                },
            //                                                                TransactionScopeAsyncFlowOption.Enabled))
            {
                await _concurrencyProcessor.ProcessAsync(async () =>
                {
                    var account = await _domainRepository.FindAsync<Account>(a => a.UserName == "ivan");
                   // var account = await _dbContext.Accounts.FindAsync(new Guid("d561edb4-f233-4ccc-bbd5-3a7badfdd65b"));
                    if (account == null)
                    {
                        throw new DomainException(1, "UserNotExists");
                    }
                    account.Modify($"ivan@163.com{DateTime.Now}");
                    await _unitOfWork.CommitAsync();
                });
               // transactionScope.Complete();
            }

        }

        public IActionResult Test()
        {
            return View();
        }


        public ApiResult PostAddRequest([FromBody]AddRequest request)
        {
            request = request ?? new AddRequest();
            request.File = Request.HasFormContentType ? Request.Form.Files.FirstOrDefault()?.FileName : null;
            return new ApiResult<AddRequest>(request);
        }

        public IActionResult Index([FromQuery]bool needGc)
        {
            using (_logger.BeginScope(new Dictionary<string, object> { { "needGc", needGc } }))
            {
                var profile = Configuration.Get("Debug");
                var member = Configuration.Get("Member:A");
                _logger.LogDebug(new { profile, member });
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
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
