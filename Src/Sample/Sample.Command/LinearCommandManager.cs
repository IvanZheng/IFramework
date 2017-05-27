namespace Sample.Command
{
    public class LinearCommandManager : IFramework.Command.Impl.LinearCommandManager
    {
        public LinearCommandManager()
        {
            RegisterLinearCommand<Login>(cmd => cmd.UserName);
            RegisterLinearCommand<Register>(cmd => "register:" + cmd.UserName);
        }
    }
}