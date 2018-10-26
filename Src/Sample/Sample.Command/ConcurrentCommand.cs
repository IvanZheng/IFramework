using IFramework.Command;
using IFramework.Infrastructure;
using IFramework.Message;

namespace Sample.Command
{
    [Topic("commandqueue")]
    public class ConcurrentCommand : ICommand
    {
        public ConcurrentCommand()
        {
            NeedRetry = true;
            Id = ObjectId.GenerateNewId().ToString();
        }

        public bool NeedRetry { get; set; }

        public string Id { get; set; }

        public string Key { get; set; }
        public string[] Tags { get; set; }

    }
}