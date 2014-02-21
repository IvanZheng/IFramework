using IFramework.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sample.ApplicationEvent
{
    public class AccountLogined : IApplicationEvent
    {
        public Guid AccountID { get; set; }
        public DateTime LoginTime { get; set; }
    }
}
