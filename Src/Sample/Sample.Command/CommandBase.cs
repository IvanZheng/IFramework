using IFramework.Command;
using IFramework.Infrastructure;
using IFramework.Message;

namespace Sample.Command
{
    [Topic("commandqueue")]
    public abstract class CommandBase : ICommand
    {
        protected CommandBase()
        {
            Id = ObjectId.GenerateNewId().ToString();
        }
        public string Id { get; set; }

        public string Key { get; set; }
        public string[] Tags { get; set; }
    }

    public abstract class LinearCommandBase : CommandBase, ILinearCommand { }
}