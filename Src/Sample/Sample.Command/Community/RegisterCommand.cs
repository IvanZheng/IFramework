using IFramework.Command;
using Sample.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Sample.Command
{
    [Serializable]
    public class RegisterCommand : ICommand<Guid>, ILinearCommand
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public Guid Result{get;set;}
    }
}
