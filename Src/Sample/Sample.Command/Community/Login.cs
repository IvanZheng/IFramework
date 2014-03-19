using IFramework.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sample.DTO;
using System.ComponentModel.DataAnnotations;

namespace Sample.Command
{
    public class Login : LinearCommandBase
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
