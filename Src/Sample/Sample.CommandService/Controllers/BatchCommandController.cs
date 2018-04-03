using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using IFramework.Infrastructure;
using Sample.DTO;
using Sample.Persistence;
using IFramework.Command;
using IFramework.DependencyInjection;
using IFramework.Message;

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
    public class BatchCommandController : ApiControllerBase
    {
        private static List<ICommand> _batchCommands = new List<ICommand>();
        private IMessagePublisher _messagePublisher = ObjectProviderFactory.GetService<IMessagePublisher>();
        private SampleModelContext _queryContext = ObjectProviderFactory.GetService<SampleModelContext>();

        public BatchCommandController(ICommandBus commandBus, IExceptionManager exceptionManager)
            :base(exceptionManager)
        {
            CommandBus = commandBus;
        }

        private ICommandBus CommandBus { get; }


        //[HttpGet]
        //[Route("api/BatchCommand")]
        //public ArrayModel Get([FromUri]ArrayModel model)
        //{
        //    return model;
        //}

        [HttpGet]
        [Route("api/BatchCommand/Collection")]
        public ArrayModelCollection Collection([FromUri] ArrayModelCollection models)
        {
            return models;
        }


        public ArrayModelCollection Post([FromBody] ArrayModelCollection models)
        {
            return models;
        }

        private async Task<ApiResult> Action(ICommand command)
        {
            if (ModelState.IsValid)
            {
                return await ExceptionManager.ProcessAsync(async () =>
                {
                    var messageResponse = await CommandBus.SendAsync(command, true);
                    return await messageResponse.ReadAsAsync<ApiResult>();
                });
            }
            return
                new ApiResult
                {
                    ErrorCode = (int)ErrorCode.CommandInvalid,
                    Message = string.Join(",", ModelState.Values
                                                         .SelectMany(v => v.Errors
                                                                           .Select(e => e.ErrorMessage)))
                };
        }

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

        private void DoCommand(List<ICommand> batchCommands)
        {
            batchCommands.ForEach(cmd => { Task.Factory.StartNew(() => { Action(cmd).Wait(); }); });
        }
    }
}