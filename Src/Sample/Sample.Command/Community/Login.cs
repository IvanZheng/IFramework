﻿using System.ComponentModel.DataAnnotations;

namespace Sample.Command
{
    public class Login : SerialCommandBase
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
}