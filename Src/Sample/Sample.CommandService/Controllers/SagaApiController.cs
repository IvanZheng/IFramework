using IFramework.Command;
using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Sample.CommandService.Controllers
{
    public class SagaApiController : ApiController
    {
        ICommandBus _CommandBus { get; set; }

        public SagaApiController(ICommandBus commandBus)
        {
            _CommandBus = commandBus;
        }

        public async Task<ApiResult> Post([FromBody]ICommand command)
        {
            if (ModelState.IsValid)
            {
                return await ExceptionManager.ProcessAsync(async () =>
                {
                    var response = await _CommandBus.StartSaga(command).ConfigureAwait(false);
                    return await response.Reply.ConfigureAwait(false);
                    //return await _CommandBus.ExecuteSaga(command);//, TimeSpan.FromMilliseconds(2000));
                });
            }
            else
            {
                return
                    new ApiResult
                    {
                        ErrorCode = DTO.ErrorCode.CommandInvalid,
                        Message = string.Join(",", ModelState.Values
                                                       .SelectMany(v => v.Errors
                                                                         .Select(e => e.ErrorMessage)))
                    };
            }
        }
    }
}