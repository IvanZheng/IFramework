using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Sample.CommandServiceCore.Controllers
{
    [Route("api/[controller]")]
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
    }
}