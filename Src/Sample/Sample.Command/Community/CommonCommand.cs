using System;
using System.Collections.Generic;
using System.Text;

namespace Sample.Command.Community
{
    public class CommonCommand:CommandBase
    {
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
    }
}
