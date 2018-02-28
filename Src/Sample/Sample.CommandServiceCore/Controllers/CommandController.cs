using System;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Sample.CommandServiceCore.Filters;

namespace Sample.CommandServiceCore.Controllers
{
    public class CommandController : ApiControllerBase
    {
        private readonly ICommandBus _commandBus;

        public CommandController(ICommandBus commandBus, IExceptionManager exceptionManager)
            : base(exceptionManager)
        {
            _commandBus = commandBus;
        }

        [HttpGet]
        public Task<string> Get()
        {
            return Task.FromResult("Get Success!");
        }

        [HttpPost("{commandName}")]
        public Task<ApiResult<object>> Post([FromBody] ICommand command)
        {
            return ProcessAsync(() => _commandBus.ExecuteAsync(command));
        }

        [ApiResultWrap]
        [HttpGet]
        [Route("do")]
        public async Task<string[]> Do()
        {
            return await Task.FromResult(new[] {"1", "2"});
        }
    }
}