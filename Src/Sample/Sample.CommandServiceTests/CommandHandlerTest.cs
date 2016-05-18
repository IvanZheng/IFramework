using IFramework.Command;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Unity.LifetimeManagers;
using IFramework.Message;
using IFramework.Message.Impl;
using Microsoft.Practices.Unity;

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
                ((dynamic)commandHandler).Handle((dynamic)command);
                var result = commandContext.Reply;

                return result;
            }
        }

        public object ExecuteCommand(IMessageContext commandContext)
        {
            using (var scope = IoCFactory.Instance.CurrentContainer.CreateChildContainer())
            {
                scope.RegisterInstance(typeof(IMessageContext), commandContext);
                var commandHandler = scope.Resolve<TCommandHanlder>();
                ((dynamic)commandHandler).Handle((dynamic)commandContext.Message);
                var result = commandContext.Reply;
                return result;
            }
        }
    }
}