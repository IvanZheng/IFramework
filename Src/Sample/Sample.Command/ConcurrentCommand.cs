using IFramework.Command;
using IFramework.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.Command
{
    public class ConcurrentCommand : ICommand
    {
        public bool NeedRetry
        {
            get;
            set;
        }

        public ConcurrentCommand()
        {
            NeedRetry = true;
            ID = ObjectId.GenerateNewId().ToString();
        }

        public string ID
        {
            get;
            set;
        }

        public string Key
        {
            get;set;
        }
    }
}
