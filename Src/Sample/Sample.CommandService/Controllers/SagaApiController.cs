using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using IFramework.Command;
using IFramework.Infrastructure;
using Sample.DTO;

namespace Sample.CommandService.Controllers
{
    public class SagaApiController : ApiController
    {
        public SagaApiController(ICommandBus commandBus)
        {
            _CommandBus = commandBus;
        }

        private ICommandBus _CommandBus { get; }

        public async Task<ApiResult> Post([FromBody] ICommand command)
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
}