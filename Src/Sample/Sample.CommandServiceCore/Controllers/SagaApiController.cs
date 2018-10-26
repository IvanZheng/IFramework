using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Sample.CommandServiceCore.Controllers
{
    public class SagaApiController : ApiControllerBase
    {
        private readonly ICommandBus _commandBus;

        public SagaApiController(ICommandBus commandBus, IConcurrencyProcessor concurrencyProcessor)
            : base(concurrencyProcessor)
        {
            _commandBus = commandBus;
        }

        [HttpPost("{commandName}")]
        public Task<object> Post([FromBody] ICommand command)
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