using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sample.DTO;

namespace Sample.Command
{
    public class Login : LinearCommandBase
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
