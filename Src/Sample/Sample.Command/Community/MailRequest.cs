using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Sample.Command.Community
{
    public class MailboxRequest
    {
        [Required]
        public string Id { get; set; }
        public int Number { get; set; }
    }
}
