using IFramework.Command;
using IFramework.Infrastructure;

namespace MSKafka.Test
{
    public class Command : ICommand
    {
        public Command(string body)
        {
            Body = body;
            NeedRetry = false;
            ID = ObjectId.GenerateNewId().ToString();
        }

        public string Body { get; set; }
        public bool NeedRetry { get; set; }
        public string Key { get; set; }

        public string ID { get; set; }
    }

    public class LinearCommand : Command, ILinearCommand
    {
        public LinearCommand(string body) : base(body)
        {
        }
    }
}