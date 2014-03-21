using IFramework.Command;
using Sample.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

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

    }
}
