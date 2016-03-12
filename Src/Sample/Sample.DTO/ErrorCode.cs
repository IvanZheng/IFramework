using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.DTO
{
    public enum ErrorCode
    {
        NoError,
        UsernameAlreadyExists,
        WrongUsernameOrPassword,
        UserNotExists,
        CountNotEnougth,
        CommandInvalid = 0x7ffffffe,
        UnknownError = 0x7fffffff
    }
}
