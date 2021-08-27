using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Sample.CommandServiceCore.Controllers
{
    public class CommandController : ApiControllerBase
    {
        private readonly ICommandBus _commandBus;
        private readonly ILogger<CommandController> _logger;

        public CommandController(ICommandBus commandBus, IConcurrencyProcessor concurrencyProcessor, ILogger<CommandController> logger)
            : base(concurrencyProcessor)
        {
            _commandBus = commandBus;
            _logger = logger;
        }

        [HttpGet]
        public Task<string> Get()
        {
            _logger.LogDebug(new Dictionary<string, string>
            {
                {"id", "name"}
            });

            _logger.LogDebug(new {Test = "aaaa"});
            try
            {
                throw new DomainException(ErrorCode.InvalidParameters, "test invalid parameter exception");
            }
            catch (Exception e)
            {
                _logger.LogError(e);

            }
            return Task.FromResult("Get Success!");
        }

        [HttpPost("{commandName}")]
        public Task<object> Post([FromBody] ICommand command)
        {
            return _commandBus.ExecuteAsync(command);
        }

        [HttpPost("send/{commandName}")]
        public async Task<object> Send([FromBody] ICommand command)
        {
            var sendResponse = await _commandBus.SendAsync(command)
                                                .ConfigureAwait(false);
            return sendResponse.MessageContext.MessageId;
        }
    }
}