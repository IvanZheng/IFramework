using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Command;

namespace IFramework.Test.Commands
{
    public class ModifyUser: ICommand
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string[] Tags { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Topic { get; set; }

    }
}
