using IFramework.Command;
using Sample.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using IFramework.Infrastructure.Mvc;
using System.Net.Http.Headers;

namespace Sample.CommandService.Controllers
{
    public class CommandController : ApiController
    {
       ICommandBus _CommandBus { get; set; }

        public CommandController(ICommandBus commandBus)
        {
            _CommandBus = commandBus;
        }

        public Task<ApiResult> Post([FromBody]ICommand command)
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


        public Task<ApiResult> Put([FromBody]ICommand command)
        {
            if (ModelState.IsValid)
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(Request.RequestUri.ToString());
                return client.DoCommand<ApiResult>(command);
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
    }
}
