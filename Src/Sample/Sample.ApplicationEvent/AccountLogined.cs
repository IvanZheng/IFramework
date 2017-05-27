using System;

namespace Sample.ApplicationEvent
{
    public class AccountLogined : ApplicationEvent
    {
        public Guid AccountID { get; set; }
        public DateTime LoginTime { get; set; }
    }
}