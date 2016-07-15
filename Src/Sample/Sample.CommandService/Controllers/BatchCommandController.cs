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
using Sample.ApplicationEvent;
using IFramework.IoC;

namespace Sample.CommandService.Controllers
{
    public class ArrayModelCollection
    {
        public ArrayModel[] ArrayModels { get; set; }
    }
    public class ArrayModel
    {
        public string[] Ids { get; set; }
        public DateTime DateTime { get; set; }
        public ArrayModel[] ArrayModels { get; set; }

    }

    [AllowAnonymous]
    public class BatchCommandController : ApiController
    {
        SampleModelContext _QueryContext = IoCFactory.Resolve<SampleModelContext>();
        IMessagePublisher _MessagePublisher = IoCFactory.Resolve<IMessagePublisher>();
        ICommandBus _CommandBus { get; set; }

        public BatchCommandController(ICommandBus commandBus)
        {
            _CommandBus = commandBus;
        }


        //[HttpGet]
        //[Route("api/BatchCommand")]
        //public ArrayModel Get([FromUri]ArrayModel model)
        //{
        //    return model;
        //}

        [HttpGet]
        [Route("api/BatchCommand/Collection")]
        public ArrayModelCollection Collection([FromUri]ArrayModelCollection models)
        {
            return models;
        }


        public ArrayModelCollection Post([FromBody]ArrayModelCollection models)
        {
            return models;
        }

        async Task<ApiResult> Action(ICommand command)
        {
            if (ModelState.IsValid)
            {
                return await ExceptionManager.ProcessAsync(async () =>
                {
                    var messageResponse = await _CommandBus.SendAsync(command);
                    return await messageResponse.ReadAsAsync<ApiResult>();
                });
            }
            else
            {
                return
                    new ApiResult
                    {
                        errorCode = DTO.ErrorCode.CommandInvalid,
                        message = string.Join(",", ModelState.Values
                                                       .SelectMany(v => v.Errors
                                                                         .Select(e => e.ErrorMessage)))
                    };
            }

        }

        static List<ICommand> BatchCommands = new List<ICommand>();

        //public Task<ApiResult> Post([FromBody]ICommand command)
        //{
        //    var batchCount = 1;
        //    int.TryParse(CookiesHelper.GetCookie("batchCount").Value, out batchCount);
        //    return Task.Factory.StartNew(() =>
        //    {
        //        BatchCommands.Add(command);
        //        if (BatchCommands.Count >= 3)
        //        {
        //            int i = 0;
        //            try
        //            {
        //                var commands = new List<ICommand>(BatchCommands);
        //                while (i++ < batchCount)
        //                {
        //                    DoCommand(commands);
        //                }
        //            }
        //            catch (Exception e)
        //            {
        //                return new ApiResult { errorCode = DTO.ErrorCode.UnknownError, message = e.GetBaseException().Message };
        //            }
        //            BatchCommands.Clear();
        //        }
        //        return new ApiResult();
        //    });

        //}

        //ApiResult Login(Login loginCommand)
        //{
        //    ApiResult result = null;
        //    var account = _QueryContext.Accounts.FirstOrDefault(a => a.UserName.Equals(loginCommand.UserName)
        //                                                          && a.Password.Equals(loginCommand.Password));
        //    if (account != null)
        //    {
        //        // 1.
        //        // Regard command controller as the application service
        //        // where is the best place to publish application events.
        //        // AccountLogined is a application event not a domain event, 
        //        // so we won't use command bus to envoke domain layer.
        //        // 2.
        //        // if regard CommandHandler as a kind of application service,
        //        // we can use command bus to envoke domain layer and 
        //        // no need to define the "Login" action of controller
        //        _MessagePublisher.Send(new AccountLogined { AccountID = account.ID, LoginTime = DateTime.Now });
        //        result = new ApiResult<Guid> { result = account.ID };
        //    }
        //    else
        //    {
        //        result = new ApiResult
        //        {
        //            errorCode = DTO.ErrorCode.WrongUsernameOrPassword,
        //            message = DTO.ErrorCode.WrongUsernameOrPassword.ToString()
        //        };
        //    }
        //    return result;
        //}

        void DoCommand(List<ICommand> batchCommands)
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