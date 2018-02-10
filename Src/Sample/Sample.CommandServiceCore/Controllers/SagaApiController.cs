using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Sample.CommandServiceCore.Controllers
{
    [Route("api/[controller]")]
    public class SagaApiController : ApiControllerBase
    {
        private readonly ICommandBus _commandBus;

        public SagaApiController(ICommandBus commandBus, IExceptionManager exceptionManager)
            : base(exceptionManager)
        {
            _commandBus = commandBus;
        }

        [HttpPost("{commandName}")]
        public Task<ApiResult<object>> Post([FromBody] ICommand command)
        {
            return ProcessAsync(async () =>
            {
                var response = await _commandBus.StartSaga(command).ConfigureAwait(false);
                return await response.Reply.ConfigureAwait(false);
                //return await _CommandBus.ExecuteSaga(command);//, TimeSpan.FromMilliseconds(2000));
            });
        }
    }
}