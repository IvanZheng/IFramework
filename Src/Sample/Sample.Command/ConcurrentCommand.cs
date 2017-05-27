using IFramework.Command;
using IFramework.Infrastructure;
using IFramework.Message;

namespace Sample.Command
{
    [Topic("commandqueueC")]
    public class ConcurrentCommand : ICommand
    {
        public ConcurrentCommand()
        {
            NeedRetry = true;
            ID = ObjectId.GenerateNewId().ToString();
        }

        public bool NeedRetry { get; set; }

        public string ID { get; set; }

        public string Key { get; set; }
    }
}