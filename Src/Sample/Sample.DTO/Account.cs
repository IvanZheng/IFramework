using System;

namespace Sample.DTO
{
    public class Account
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public CommonStatus Status { get; set; } = CommonStatus.Normal;
    }
}