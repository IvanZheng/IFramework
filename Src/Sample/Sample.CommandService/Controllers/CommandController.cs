using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using IFramework.Infrastructure;
using System.Threading.Tasks;
using System.Threading;
using IFramework.Message;
using Sample.Command;
using Sample.Persistence;
using IFramework.Event;
using Sample.ApplicationEvent;
using Sample.Persistence.Repositories;
using IFramework.UnitOfWork;

namespace Sample.CommandService.Controllers
{
    public class CommandController : ApiController
    {
        SampleModelContext _QueryContext = IoCFactory.Resolve<SampleModelContext>();
        IEventPublisher _EventPublisher = IoCFactory.Resolve<IEventPublisher>();
        ICommandBus _CommandBus { get; set; }
        public CommandController(ICommandBus commandBus)
        {
            _CommandBus = commandBus;
        }

        public Task<ApiResult> Action(ICommand command)
        {
            if (ModelState.IsValid)
            {
                return ExceptionManager.Process(_CommandBus.Send(command));
            }
            else
            {
                return Task.Factory.StartNew<ApiResult>(() =>
                    new ApiResult
                    {
                        ErrorCode = ErrorCode.CommandInvalid,
                        Message = string.Join(",", ModelState.Values
                                                       .SelectMany(v => v.Errors
                                                                         .Select(e => e.ErrorMessage)))
                    });
            }
            
        }

        static List<ICommand> BatchCommands = new List<ICommand>();
        public ApiResult TestDistributor(ICommand command)
        {
            BatchCommands.Add(command);
            if (BatchCommands.Count >= 3)
            {
                var batchCount = 1;
                int.TryParse(CookiesHelper.GetCookie("batchCount").Value, out batchCount);
                int i = 0;
                try
                {
                    var commands = new List<ICommand>(BatchCommands);
                    while (i++ < batchCount)
                    {
                        DoCommand(commands);
                    }
                }
                catch (Exception e)
                {
                    return new ApiResult { ErrorCode = ErrorCode.UnknownError, Message = e.GetBaseException().Message };
                }
                BatchCommands.Clear();
            }
            return new ApiResult();
        }

        public ApiResult Login(Login loginCommand)
        {
            ApiResult result = null;
            var account = _QueryContext.Accounts.FirstOrDefault(a => a.UserName.Equals(loginCommand.UserName)
                                                                  && a.Password.Equals(loginCommand.Password));
            if (account != null)
            {
                // 1.
                // Regard command controller as the application service
                // where is the best place to publish application events.
                // AccountLogined is a application event not a domain event, 
                // so we won't use command bus to envoke domain layer.
                // 2.
                // if regard CommandHandler as a kind of application service,
                // we can use command bus to envoke domain layer and 
                // no need to define the "Login" action of controller
                _EventPublisher.Publish(new AccountLogined { AccountID = account.ID, LoginTime = DateTime.Now });
                result = new ApiResult<Guid> { Result = account.ID };
            }
            else
            {
                result = new ApiResult { ErrorCode = ErrorCode.WrongUsernameOrPassword, 
                                         Message = ErrorCode.WrongUsernameOrPassword.ToString() };
            }
            return result;
        }

        [HttpGet]
        public Task<string> CommandDistributorStatus()
        {
            return Task.Factory.StartNew(() =>
            {
                var commandDistributor = IoCFactory.Resolve<IMessageConsumer>("CommandDistributor");
                var domainEventConsumer = IoCFactory.Resolve<IMessageConsumer>("DomainEventConsumer");
                var distributorStatus = commandDistributor.GetStatus() +
                    "event consumer:" + domainEventConsumer.GetStatus();
                return distributorStatus;
            });

        }

        public void DoCommand(List<ICommand> batchCommands)
        {
            batchCommands.ForEach(cmd =>
            {
                Task.Factory.StartNew(() =>
                {
                    Action(cmd);
                });
            });
        }
    }
}