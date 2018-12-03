using System;
using System.Threading.Tasks;
using IFramework.AspNet;
using IFramework.Command;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Sample.CommandServiceCore.Filters;

namespace Sample.CommandServiceCore.Controllers
{
    public class CommandController : ApiControllerBase
    {
        private readonly ICommandBus _commandBus;

        public CommandController(ICommandBus commandBus, IConcurrencyProcessor concurrencyProcessor)
            : base(concurrencyProcessor)
        {
            _commandBus = commandBus;
        }
        
        public Task<string> Get()
        {
            return Task.FromResult("Get Success!");
        }

        [HttpPost("{commandName}")]
        public Task<object> Post([FromBody] ICommand command) => _commandBus.ExecuteAsync(command);

        [HttpPost("send/{commandName}")]
        public async Task<object> Send([FromBody] ICommand command)
        {
            var sendResponse = await _commandBus.SendAsync(command)
                                                .ConfigureAwait(false);
            return sendResponse.MessageContext;
        }
    }
}