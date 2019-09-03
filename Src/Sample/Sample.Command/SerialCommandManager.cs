namespace Sample.Command
{
    public class SerialCommandManager : IFramework.Command.Impl.SerialCommandManager
    {
        public SerialCommandManager()
        {
            RegisterSerialCommand<Login>(cmd => cmd.UserName);
            RegisterSerialCommand<Register>(cmd => "register:" + cmd.UserName);
        }
    }
}