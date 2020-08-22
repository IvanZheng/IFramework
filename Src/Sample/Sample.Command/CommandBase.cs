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
        public virtual string Id { get; set; }

        public virtual string Key { get; set; }
        public virtual string[] Tags { get; set; }
    }

    public abstract class SerialCommandBase : CommandBase, ILinearCommand { }
}