using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using IFramework.AspNet;
using IFramework.Command;
using IFramework.Infrastructure;
using Sample.DTO;

namespace Sample.CommandService.Controllers
{
    public class CommandController : ApiControllerBase
    {
        public CommandController(ICommandBus commandBus, IExceptionManager exceptionManager)
            : base(exceptionManager)
        {
            _CommandBus = commandBus;
        }

        private ICommandBus _CommandBus { get; }

        public Task<ApiResult<object>> Post([FromBody] ICommand command)
        {
            return ProcessAsync(() => _CommandBus.ExecuteAsync(command));

            //if (ModelState.IsValid)
            //{
            //    return await ExceptionManager.;
            //}
            //return
            //    new ApiResult
            //    {
            //        ErrorCode = ErrorCode.CommandInvalid,
            //        Message = string.Join(",", ModelState.Values
            //                                             .SelectMany(v => v.Errors
            //                                                               .Select(e => e.ErrorMessage)))
            //    };
        }


        public Task<ApiResult> Put([FromBody] ICommand command)
        {
            if (ModelState.IsValid)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(string.Format("{0}://{1}/api/command",
                                                               Request.RequestUri.Scheme,
                                                               Request.RequestUri.Authority));
                    return client.DoCommand<ApiResult>(command, null);
                }
            }
            return Task.Factory.StartNew(() =>
                                             new ApiResult
                                             {
                                                 ErrorCode = (int)ErrorCode.CommandInvalid,
                                                 Message = string.Join(",", ModelState.Values
                                                                                      .SelectMany(v => v.Errors
                                                                                                        .Select(e => e.ErrorMessage)))
                                             });
        }
    }
}