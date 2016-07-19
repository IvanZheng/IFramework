using IFramework.Command;
using Sample.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Net.Http.Headers;
using IFramework.AspNet;
using IFramework.Infrastructure;

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
                return await ExceptionManager.ProcessAsync(async () =>
                {
                    return await _CommandBus.Execute(command);//, TimeSpan.FromMilliseconds(2000));
                    //var messageResponse = await _CommandBus.SendAsync(command, TimeSpan.FromSeconds(5));
                    //return await messageResponse.Reply.Timeout(TimeSpan.FromMilliseconds(2000));
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
                        errorCode = DTO.ErrorCode.CommandInvalid,
                        message = string.Join(",", ModelState.Values
                                                       .SelectMany(v => v.Errors
                                                                         .Select(e => e.ErrorMessage)))
                    });
            }
        }
    }
}
