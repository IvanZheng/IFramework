using IFramework.Command;

namespace Sample.Command
{
    public class Modify : ConcurrentCommand, ILinearCommand
    {
        public string UserName { get; set; }
        public string Email { get; set; }
    }
}