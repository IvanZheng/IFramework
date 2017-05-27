using System.Threading.Tasks;
using IFramework.Command;
using IFramework.IoC;
using IFramework.Message;
using IFramework.Message.Impl;

namespace Sample.CommandServiceTests
{
    public class CommandHandlerProxy<TCommandHanlder>
    {
        public object ExecuteCommand(ICommand command)
        {
            IMessageContext commandContext = new EmptyMessageContext(command);
            using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
            {
                scope.RegisterInstance(typeof(IMessageContext), commandContext);
                var commandHandler = scope.Resolve<TCommandHanlder>();
                ((dynamic) commandHandler).Handle((dynamic) command);
                var result = commandContext.Reply;

                return result;
            }
        }

        public async Task ExecuteCommand(IMessageContext commandContext)
        {
            using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
            {
                scope.RegisterInstance(typeof(IMessageContext), commandContext);
                var commandHandler = scope.Resolve<TCommandHanlder>();
                await Task.Run(((dynamic) commandHandler).Handle((dynamic) commandContext.Message));
                var result = commandContext.Reply;
            }
        }
    }
}