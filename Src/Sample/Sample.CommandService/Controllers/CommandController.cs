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

        public async Task<ApiResult> Post([FromBody]ICommand command)
        {
            if (ModelState.IsValid)
            {
                return await ExceptionManager.Process(() => _CommandBus.Send(command));
            }
            else
            {
                return 
                    new ApiResult
                    {
                        ErrorCode = ErrorCode.CommandInvalid,
                        Message = string.Join(",", ModelState.Values
                                                       .SelectMany(v => v.Errors
                                                                         .Select(e => e.ErrorMessage)))
                    };
            }
        }


        public Task<ApiResult> Put([FromBody]ICommand command)
        {
            if (ModelState.IsValid)
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(string.Format("{0}://{1}/api/command", 
                    Request.RequestUri.Scheme, 
                    Request.RequestUri.Authority));
                return client.DoCommand<ApiResult>(command, null);
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
