using System;

namespace Sample.ApplicationEvent
{
    public class AccountLogined : ApplicationEvent
    {
        public Guid AccountId { get; set; }
        public DateTime LoginTime { get; set; }
    }
}