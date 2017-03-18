using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.DTO
{
    public enum ErrorCode
    {
        NoError,
        UsernameAlreadyExists,
        [Description("username:{0} or password is wrong!")]
        WrongUsernameOrPassword,
        UserNotExists,
        CountNotEnougth,
        CommandInvalid = 0x7ffffffe,
        UnknownError = 0x7fffffff
    }
}
