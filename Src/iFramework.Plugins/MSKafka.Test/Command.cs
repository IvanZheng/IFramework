using IFramework.Command;
using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSKafka.Test
{
    public class Command : ICommand
    {
        public bool NeedRetry { get; set; }
        public string Body { get; set; }
        public string Key { get; set; }
        public Command(string body)
        {
            Body = body;
            NeedRetry = false;
            ID = ObjectId.GenerateNewId().ToString();
        }

        public string ID
        {
            get;
            set;
        }
    }

    public class LinearCommand : Command, ILinearCommand
    {
        public LinearCommand(string body) : base(body) { }
    }
}
