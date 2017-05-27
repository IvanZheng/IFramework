using System.ComponentModel;

namespace Sample.DTO
{
    public enum ErrorCode
    {
        NoError,
        UsernameAlreadyExists,
        [Description("username:{0} or password is wrong!")] WrongUsernameOrPassword,
        UserNotExists,
        CountNotEnougth,
        CommandInvalid = 0x7ffffffe,
        UnknownError = 0x7fffffff
    }
}