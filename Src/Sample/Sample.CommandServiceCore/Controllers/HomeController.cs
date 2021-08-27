using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using IFramework.AspNet;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Infrastructure.EventSourcing.Repositories;
using IFramework.Infrastructure.Mailboxes;
using IFramework.MessageQueue.ConfluentKafka;
using IFramework.UnitOfWork;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sample.Applications;
using Sample.Command.Community;
using Sample.CommandServiceCore.Models;
using Sample.Domain;
using Sample.Domain.Model;
using Sample.Domain.Model.Bank.Accounts;
using Sample.Persistence;

namespace Sample.CommandServiceCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConcurrencyProcessor _concurrencyProcessor;
        private readonly SampleModelContext _dbContext;
        private readonly ICommunityService _communityService;
        private readonly IMailboxProcessor _mailboxProcessor;
        private readonly KafkaManager _kafkaManager;
        private readonly ICommunityRepository _domainRepository;
        private readonly ILogger _logger;
        private readonly IObjectProvider _objectProvider;
        private readonly IUnitOfWork _unitOfWork;
        //private readonly IEventSourcingRepository<BankAccount> _bankAccountRepository;
        public HomeController(IConcurrencyProcessor concurrencyProcessor,
                              ILogger<HomeController> logger,
                              IObjectProvider objectProvider,
                              IUnitOfWork unitOfWork,
                              ICommunityRepository domainRepository,
                              SampleModelContext dbContext,
                              ICommunityService communityService,
                              IMailboxProcessor mailboxProcessor,
                              KafkaManager kafkaManager
                              //IEventSourcingRepository<BankAccount> bankAccountRepository
            )
        {
            //_bankAccountRepository = bankAccountRepository;
            _concurrencyProcessor = concurrencyProcessor;
            _objectProvider = objectProvider;
            _unitOfWork = unitOfWork;
            _domainRepository = domainRepository;
            _dbContext = dbContext;
            _communityService = communityService;
            _mailboxProcessor = mailboxProcessor;
            _kafkaManager = kafkaManager;
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
            //var userId = new Guid("4ED7460E-C914-45A6-B1C9-4DC97C5D52D0");
            //await _communityService.ModifyUserEmailAsync(userId, $"{DateTime.Now.Ticks}");

            var version = await _communityService.ModifyUserEmailAsync(Guid.Empty, $"{DateTime.Now.Ticks}");
            return $"{DateTime.Now} version:{version} DoApi Done! sameProvider:{sameProvider} ";
        }

        public string Gc()
        {
            GC.Collect();
            return "Gc done!";
        }

        //[Route("home/getBankAccount/{accountId}")]
        //public Task<BankAccount> GetBankAccount(string accountId)
        //{
        //    return _bankAccountRepository.GetByKeyAsync(accountId);
        //}

        public IActionResult Test()
        {
            ViewBag.MailboxValue = _communityService.GetMailboxValues().ToJson();
            ViewBag.MailboxStatus = _mailboxProcessor.Status;
            return View();
        }

        [HttpGet]
        public object ConsumerOffsets(string topic, string group)
        {
            var offsets = _kafkaManager.GetTopicInfo(topic, group);
            return new
            {
                TotalLag = offsets.Sum(o => o.Lag),
                Offset = offsets.Sum(o => o.Offset < 0 ? 0 : o.Offset),
                offsets
            };
        }

        [ApiResultWrap]
        public async Task<object> MailboxTest([FromBody] MailboxRequest request)
        {
            var result = await _communityService.MailboxTestAsync(request);
            //var result = await _communityService.ModifyUserEmailAsync(Guid.Empty, DateTime.Now.ToString(CultureInfo.InvariantCulture));
            ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
            return new {result, workerThreads, completionPortThreads};
        }

        public ApiResult PostAddRequest([FromBody] AddRequest request)
        {
            request = request ?? new AddRequest();
            request.File = Request.HasFormContentType ? Request.Form.Files.FirstOrDefault()?.FileName : null;
            return new ApiResult<AddRequest>(request);
        }

        public IActionResult Index([FromQuery] bool needGc)
        {
            _logger.SetMinLevel(LogLevel.Debug);
            _logger.LogDebug("index test start");
            using (_logger.BeginScope("scope1"))
            using (_logger.BeginScope("scope2"))
                using (_logger.BeginScope(new Dictionary<string, object>{ {"name",  "scope1"}}))
            using (_logger.BeginScope(new Dictionary<string, object> { { "needGc", needGc } }))
            {
                var profile = Configuration.Instance.Get("Debug");
                var member = Configuration.Instance.Get("Member:A");
                _logger.LogInformation(new { profile, member });
            }
            _logger.LogWarning(new {text = "index test end"});

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
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}