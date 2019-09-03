namespace Sample.Command
{
    public class Register : LinearCommandBase
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public Tag[] TagCollection { get; set; }
    }

    public class Tag
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}